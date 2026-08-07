namespace Icbank.Platform.Application.Gac;

/// <summary>Ports a single row of <c>gac_publications</c> for API responses (API-SURFACE.md §12).</summary>
/// <param name="Id">The publication id.</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="TitleEn">The optional English title.</param>
/// <param name="Category">The publication category.</param>
/// <param name="Language">The primary language.</param>
/// <param name="DescriptionAr">The optional Arabic description.</param>
/// <param name="DescriptionEn">The optional English description.</param>
/// <param name="FileUrl">The file's storage URL.</param>
/// <param name="FileSizeBytes">The file size in bytes, if known.</param>
/// <param name="PageCount">The page count, if known.</param>
/// <param name="Tags">The searchable tag list.</param>
/// <param name="SourceDomain">The domain the publication was sourced from.</param>
/// <param name="Status">The lifecycle status.</param>
/// <param name="DisplayOrder">The display order.</param>
/// <param name="PublishedAt">The UTC timestamp of original publication, if known.</param>
public sealed record GacPublicationDto(
    int Id,
    string TitleAr,
    string? TitleEn,
    string Category,
    string Language,
    string? DescriptionAr,
    string? DescriptionEn,
    string FileUrl,
    int? FileSizeBytes,
    int? PageCount,
    IReadOnlyList<string> Tags,
    string SourceDomain,
    string Status,
    int DisplayOrder,
    DateTimeOffset? PublishedAt);
