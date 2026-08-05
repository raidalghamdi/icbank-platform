using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>PATCH /weekend-places/:id</c> (API-SURFACE.md §9). Admin-only. Every field is optional (partial update).</summary>
/// <param name="ActorUserId">The updating admin's id, for the audit-log write.</param>
/// <param name="PlaceId">The place being updated.</param>
/// <param name="Name">The new name, if changing.</param>
/// <param name="Description">The new description, if changing.</param>
/// <param name="ImageUrl">The new image URL, if changing.</param>
/// <param name="City">The new city, if changing.</param>
/// <param name="MapsQuery">The new Maps query, if changing.</param>
/// <param name="IsActive">The new active flag, if changing.</param>
/// <param name="SortOrder">The new sort order, if changing.</param>
public sealed record UpdateWeekendPlaceCommand(
    int ActorUserId,
    int PlaceId,
    string? Name,
    string? Description,
    string? ImageUrl,
    string? City,
    string? MapsQuery,
    bool? IsActive,
    int? SortOrder)
    : IRequest<Result<WeekendPlaceDto>>;
