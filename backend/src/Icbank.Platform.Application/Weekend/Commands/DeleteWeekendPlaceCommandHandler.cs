using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>
/// Handles <see cref="DeleteWeekendPlaceCommand"/>. The Node source additionally best-effort
/// deleted the place's storage object if its <c>imageUrl</c> pointed under
/// <c>/objects/weekend/</c> — this port does not perform that storage side-effect (no storage
/// write/delete port exists yet for the weekend/ prefix, only the read-side
/// <see cref="Icbank.Platform.Application.Storage.IObjectStorageReader"/>), so a deleted place's
/// underlying image object is orphaned in storage rather than cleaned up. Deferred, see
/// WAVE1-PORT-NOTES.md.
/// </summary>
public sealed class DeleteWeekendPlaceCommandHandler : IRequestHandler<DeleteWeekendPlaceCommand, Result<bool>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="DeleteWeekendPlaceCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public DeleteWeekendPlaceCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<bool>> Handle(DeleteWeekendPlaceCommand request, CancellationToken cancellationToken)
    {
        WeekendPlace? place = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.WeekendPlaces.Where(p => p.Id == request.PlaceId), cancellationToken);
        if (place is null)
        {
            return Result<bool>.Failure("المكان غير موجود");
        }

        _dbContext.Remove(place);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "weekend_place.delete",
            "WeekendPlace",
            request.PlaceId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: new { place.Name },
            after: null,
            cancellationToken);

        return Result<bool>.Success(true);
    }
}
