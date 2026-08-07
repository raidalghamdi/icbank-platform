namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>One news item in an ingest batch.</summary>
/// <param name="TitleAr">The Arabic headline.</param>
/// <param name="BodyAr">The Arabic body, when available.</param>
/// <param name="SourceUrl">The canonical article URL, used as the deduplication key.</param>
/// <param name="SourceName">The publishing outlet's display name.</param>
/// <param name="PublishedAt">The publication timestamp, when known.</param>
/// <param name="Kind">The item kind, parsed against <see cref="Domain.Gac.GacNewsKind"/>; defaults to News.</param>
/// <param name="Category">The item category, parsed against <see cref="Domain.Gac.GacNewsCategory"/>; optional.</param>
/// <param name="Tags">Free-form searchable tags.</param>
public sealed record IngestGacNewsItem(
    string TitleAr,
    string? BodyAr,
    string SourceUrl,
    string? SourceName,
    DateTimeOffset? PublishedAt,
    string? Kind,
    string? Category,
    IReadOnlyList<string>? Tags);
