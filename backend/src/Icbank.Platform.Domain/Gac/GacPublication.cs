using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Gac;

/// <summary>GAC's official publication library (DATA-MODEL.md section 3.5 <c>gac_publications</c>).</summary>
public sealed class GacPublication : AuditableEntity
{
    /// <summary>Gets or sets the Arabic title.</summary>
    public string TitleAr { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional English title.</summary>
    public string? TitleEn { get; set; }

    /// <summary>Gets or sets the publication category.</summary>
    public GacPublicationCategory Category { get; set; }

    /// <summary>Gets or sets the primary language.</summary>
    public GacPublicationLanguage Language { get; set; } = GacPublicationLanguage.Ar;

    /// <summary>Gets or sets the optional Arabic description.</summary>
    public string? DescriptionAr { get; set; }

    /// <summary>Gets or sets the optional English description.</summary>
    public string? DescriptionEn { get; set; }

    /// <summary>Gets or sets the version/edition label, if any.</summary>
    public string? Version { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the original publication, if known.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Gets or sets the original source URL, for reference.</summary>
    public string? OriginalUrl { get; set; }

    /// <summary>Gets or sets the file's storage URL.</summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the file size in bytes, if known.</summary>
    public int? FileSizeBytes { get; set; }

    /// <summary>Gets or sets the page count, if known.</summary>
    public int? PageCount { get; set; }

    /// <summary>Gets or sets the searchable tag list.</summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>Gets or sets the domain the publication was sourced from.</summary>
    public GacPublicationSourceDomain SourceDomain { get; set; } = GacPublicationSourceDomain.Gacbep;

    /// <summary>Gets or sets the lifecycle status.</summary>
    public GacPublicationStatus Status { get; set; } = GacPublicationStatus.Published;

    /// <summary>Gets or sets the display order (lower sorts first).</summary>
    public int DisplayOrder { get; set; } = 100;
}
