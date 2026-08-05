using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>POST /weekend-places</c> (API-SURFACE.md §9). Admin-only.</summary>
/// <param name="ActorUserId">The creating admin's id, for the audit-log write.</param>
/// <param name="Name">The place name.</param>
/// <param name="Description">The place description.</param>
/// <param name="ImageUrl">The image URL, if any.</param>
/// <param name="City">The city (defaults to Riyadh if omitted).</param>
/// <param name="MapsQuery">The Google Maps query, if any.</param>
/// <param name="SortOrder">The display sort order.</param>
public sealed record CreateWeekendPlaceCommand(
    int ActorUserId, string Name, string Description, string? ImageUrl, string? City, string? MapsQuery, int SortOrder)
    : IRequest<Result<WeekendPlaceDto>>;
