using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Weekend;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Handles <see cref="CreateWeekendPlaceCommand"/>.</summary>
public sealed class CreateWeekendPlaceCommandHandler : IRequestHandler<CreateWeekendPlaceCommand, Result<WeekendPlaceDto>>
{
    private readonly IApplicationDbContext _dbContext;
    private readonly IAuditLogService _auditLogService;

    /// <summary>Initializes a new instance of the <see cref="CreateWeekendPlaceCommandHandler"/> class.</summary>
    /// <param name="dbContext">The persistence port.</param>
    /// <param name="auditLogService">The privileged-action audit log port.</param>
    public CreateWeekendPlaceCommandHandler(IApplicationDbContext dbContext, IAuditLogService auditLogService)
    {
        _dbContext = dbContext;
        _auditLogService = auditLogService;
    }

    /// <inheritdoc />
    public async Task<Result<WeekendPlaceDto>> Handle(CreateWeekendPlaceCommand request, CancellationToken cancellationToken)
    {
        var place = new WeekendPlace
        {
            Name = request.Name,
            Description = request.Description,
            ImageUrl = request.ImageUrl,
            City = string.IsNullOrWhiteSpace(request.City) ? "الرياض" : request.City,
            MapsQuery = request.MapsQuery,
            SortOrder = request.SortOrder,
            IsActive = true,
            CreatedBy = request.ActorUserId.ToString(System.Globalization.CultureInfo.InvariantCulture),
        };

        _dbContext.Add(place);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _auditLogService.RecordAsync(
            request.ActorUserId,
            "weekend_place.create",
            "WeekendPlace",
            place.Id.ToString(System.Globalization.CultureInfo.InvariantCulture),
            before: null,
            after: new { place.Name },
            cancellationToken);

        return Result<WeekendPlaceDto>.Success(ToDto(place));
    }

    private static WeekendPlaceDto ToDto(WeekendPlace place) =>
        new(place.Id, place.Name, place.Description, place.ImageUrl, place.City, place.MapsQuery, place.IsActive, place.SortOrder);
}
