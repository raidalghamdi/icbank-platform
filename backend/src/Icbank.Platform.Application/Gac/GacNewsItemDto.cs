namespace Icbank.Platform.Application.Gac;

/// <summary>Ports a single row of <c>gac_news_items</c> (API-SURFACE.md §12).</summary>
/// <param name="Id">The item id.</param>
/// <param name="Kind">The item kind.</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="TitleEn">The optional English title.</param>
/// <param name="BodyAr">The optional Arabic body.</param>
/// <param name="Category">The optional category.</param>
/// <param name="SourceUrl">The source URL, if any.</param>
/// <param name="SourceName">The publishing outlet, if known (stored as the item's external ref).</param>
/// <param name="PublishedAt">The UTC timestamp the item was published, if known.</param>
public sealed record GacNewsItemDto(
    int Id,
    string Kind,
    string TitleAr,
    string? TitleEn,
    string? BodyAr,
    string? Category,
    string? SourceUrl,
    string? SourceName,
    DateTimeOffset? PublishedAt);
