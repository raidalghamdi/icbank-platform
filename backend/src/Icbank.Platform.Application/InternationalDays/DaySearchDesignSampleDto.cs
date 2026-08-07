namespace Icbank.Platform.Application.InternationalDays;

/// <summary>One AI-returned design-sample entry from the search prompt's <c>design_samples</c> array (BUSINESS-RULES.md §4.2).</summary>
/// <param name="EntityName">The entity that published the design.</param>
/// <param name="EntityType">The entity type, free text (government/private/international).</param>
/// <param name="Platform">The platform the design was published on.</param>
/// <param name="Description">A description of the design/visual campaign.</param>
/// <param name="PageUrl">The page/post URL, or <c>null</c>.</param>
/// <param name="ImageUrl">A direct image URL, or <c>null</c>.</param>
/// <param name="Country">The country of origin.</param>
/// <param name="Year">The design's year.</param>
public sealed record DaySearchDesignSampleDto(
    string? EntityName,
    string? EntityType,
    string? Platform,
    string? Description,
    string? PageUrl,
    string? ImageUrl,
    string? Country,
    int? Year);
