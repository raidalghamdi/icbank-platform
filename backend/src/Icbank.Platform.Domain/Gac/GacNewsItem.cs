using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Gac;

/// <summary>Cached news/decisions feed item (DATA-MODEL.md section 3.5 <c>gac_news_items</c>).</summary>
public sealed class GacNewsItem : AuditableEntity
{
    /// <summary>Gets or sets the item kind.</summary>
    public GacNewsKind Kind { get; set; } = GacNewsKind.News;

    /// <summary>Gets or sets the Arabic title.</summary>
    public string TitleAr { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional English title.</summary>
    public string? TitleEn { get; set; }

    /// <summary>Gets or sets the optional Arabic body.</summary>
    public string? BodyAr { get; set; }

    /// <summary>Gets or sets the optional English body.</summary>
    public string? BodyEn { get; set; }

    /// <summary>Gets or sets the optional category.</summary>
    public GacNewsCategory? Category { get; set; }

    /// <summary>Gets or sets the source URL, if any.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Gets or sets the attached image URL, if any.</summary>
    public string? ImageUrl { get; set; }

    /// <summary>Gets or sets the UTC timestamp the item was published, if known.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Gets or sets an external reference id, e.g. a decision number.</summary>
    public string? ExternalRef { get; set; }

    /// <summary>Gets or sets the searchable tag list.</summary>
    public List<string> Tags { get; set; } = new();
}
