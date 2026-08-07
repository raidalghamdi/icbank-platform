namespace Icbank.Platform.Application.Weekend;

/// <summary>The weekend-place response shape (API-SURFACE.md §9).</summary>
/// <param name="Id">The place id.</param>
/// <param name="Name">The place name.</param>
/// <param name="Description">The place description.</param>
/// <param name="ImageUrl">The image URL, if any.</param>
/// <param name="City">The city.</param>
/// <param name="MapsQuery">The Google Maps query, if any.</param>
/// <param name="IsActive">Whether the place is currently shown.</param>
/// <param name="SortOrder">The display sort order.</param>
public sealed record WeekendPlaceDto(int Id, string Name, string Description, string? ImageUrl, string City, string? MapsQuery, bool IsActive, int SortOrder);
