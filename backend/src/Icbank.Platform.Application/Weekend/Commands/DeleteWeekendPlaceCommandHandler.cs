using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>
/// Handles <see cref="DeleteWeekendPlaceCommand"/>. The Node source additionally best-effort
/// deleted the place's storage object if its <c>imageUrl</c> pointed under
/// <c>/objects/weekend/</c>; this handler now performs the same cleanup via
/// <see cref="IObjectStorageDeleter"/>, closing WAVE1-PORT-NOTES.md item 23 (a deleted place no
/// longer orphans its blob). The row delete is the operation the caller asked for and must not be
/// undone by a storage-side failure, so the storage delete is attempted after the row is already
/// gone and its outcome does not affect the command's result.
/// </summary>
public sealed class DeleteWeekendPlaceCommandHandler : IRequestHandler<DeleteWeekendPlaceCommand, Result<bool>>
{
    // Why: matches the exact prefix GetWeekendPlaceUploadUrlQueryHandler issues ImageUrl values
    // under -- the only prefix this handler is ever allowed to delete from, regardless of what a
    // stored (and originally client-influenced) ImageUrl value claims.
    private static readonly string[] WeekendPrefix = { "weekend/" };

    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;
    private readonly ISafeStoragePathValidator _pathValidator;
    private readonly IObjectStorageDeleter _storageDeleter;

    /// <summary>Initializes a new instance of the <see cref="DeleteWeekendPlaceCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    /// <param name="pathValidator">The traversal-safe path validator (closes SEC-17), re-applied here because <see cref="WeekendPlace.ImageUrl"/> was itself set from earlier client input.</param>
    /// <param name="storageDeleter">The object-storage delete port.</param>
    public DeleteWeekendPlaceCommandHandler(
        IApplicationDbContext dbContext,
        IAsyncQueryExecutor queryExecutor,
        IAuditLogService auditLogService,
        ISafeStoragePathValidator pathValidator,
        IObjectStorageDeleter storageDeleter)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
        _pathValidator = pathValidator;
        _storageDeleter = storageDeleter;
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

        await TryDeleteImageAsync(place.ImageUrl, cancellationToken);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Best-effort deletes the place's storage object, if any. Never throws: a storage-side
    /// failure (transient outage, already-gone object, an <see cref="WeekendPlace.ImageUrl"/>
    /// value that does not actually point under the weekend/ prefix) must not surface as a
    /// failure of the delete command the caller already saw succeed against the database.
    /// </summary>
    private async Task TryDeleteImageAsync(string? imageUrl, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return;
        }

        SafePathValidationResult validation = _pathValidator.Validate(imageUrl, WeekendPrefix);
        if (!validation.IsValid)
        {
            return;
        }

        await _storageDeleter.DeleteAsync(validation.NormalizedPath!, cancellationToken);
    }
}
