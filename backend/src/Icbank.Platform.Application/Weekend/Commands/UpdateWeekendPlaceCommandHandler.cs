using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Handles <see cref="UpdateWeekendPlaceCommand"/>.</summary>
public sealed class UpdateWeekendPlaceCommandHandler : IRequestHandler<UpdateWeekendPlaceCommand, Result<WeekendPlaceDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAsyncQueryExecutor _queryExecutor;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="UpdateWeekendPlaceCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="queryExecutor">The async LINQ execution port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public UpdateWeekendPlaceCommandHandler(IApplicationDbContext dbContext, IAsyncQueryExecutor queryExecutor, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _queryExecutor = queryExecutor;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<WeekendPlaceDto>> Handle(UpdateWeekendPlaceCommand request, CancellationToken cancellationToken)
    {
        WeekendPlace? place = await _queryExecutor.SingleOrDefaultAsync(
            _dbContext.WeekendPlaces.Where(p => p.Id == request.PlaceId), cancellationToken);
        if (place is null)
        {
            return Result<WeekendPlaceDto>.Failure("المكان غير موجود");
        }

        ApplyChanges(place, request);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "weekend_place.update",
            "WeekendPlace",
            place.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { place.Name },
            cancellationToken);

        return Result<WeekendPlaceDto>.Success(ToDto(place));
    }

    private static void ApplyChanges(WeekendPlace place, UpdateWeekendPlaceCommand request)
    {
        place.Name = request.Name ?? place.Name;
        place.Description = request.Description ?? place.Description;
        place.ImageUrl = request.ImageUrl ?? place.ImageUrl;
        place.City = request.City ?? place.City;
        place.MapsQuery = request.MapsQuery ?? place.MapsQuery;
        place.IsActive = request.IsActive ?? place.IsActive;
        place.SortOrder = request.SortOrder ?? place.SortOrder;
    }

    private static WeekendPlaceDto ToDto(WeekendPlace place) =>
        new(place.Id, place.Name, place.Description, place.ImageUrl, place.City, place.MapsQuery, place.IsActive, place.SortOrder);
}
