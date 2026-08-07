namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>One publication metadata row to reseed.</summary>
/// <param name="TitleAr">The Arabic title (idempotency key).</param>
/// <param name="TitleEn">The optional English title.</param>
/// <param name="Category">The publication category.</param>
/// <param name="Language">The primary language.</param>
/// <param name="DescriptionAr">The optional Arabic description.</param>
/// <param name="DescriptionEn">The optional English description.</param>
/// <param name="FileUrl">The already-known storage URL for the file.</param>
/// <param name="FileSizeBytes">The file size in bytes, if known.</param>
/// <param name="PageCount">The page count, if known.</param>
/// <param name="Tags">The searchable tag list.</param>
/// <param name="SourceDomain">The domain the publication was sourced from.</param>
/// <param name="DisplayOrder">The display order.</param>
public sealed record ReseedGacPublicationItem(
    string TitleAr,
    string? TitleEn,
    string Category,
    string Language,
    string? DescriptionAr,
    string? DescriptionEn,
    string FileUrl,
    int? FileSizeBytes,
    int? PageCount,
    IReadOnlyList<string>? Tags,
    string SourceDomain,
    int DisplayOrder);
