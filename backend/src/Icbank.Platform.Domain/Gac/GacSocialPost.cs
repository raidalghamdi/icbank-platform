using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Gac;

/// <summary>
/// Cached LinkedIn/Twitter/Instagram/YouTube social feed item
/// (DATA-MODEL.md section 3.5 <c>gac_social_posts</c>).
/// </summary>
/// <remarks>
/// Deviation: the source schema comment claims <c>UNIQUE(platform, external_id)</c> but no such
/// database constraint actually exists (AMBIGUOUS-7 in DATA-MODEL.md). This port adds a real
/// unique index on (Platform, ExternalId) since duplicate ingestion has no known business value
/// and this is flagged for product confirmation in DOMAIN-PORT-NOTES.md.
/// </remarks>
public sealed class GacSocialPost : AuditableEntity
{
    /// <summary>Gets or sets the source platform.</summary>
    public GacSocialPlatform Platform { get; set; }

    /// <summary>Gets or sets the post's id on the originating platform.</summary>
    public string ExternalId { get; set; } = string.Empty;

    /// <summary>Gets or sets the Arabic post content.</summary>
    public string? ContentAr { get; set; }

    /// <summary>Gets or sets the English post content.</summary>
    public string? ContentEn { get; set; }

    /// <summary>Gets or sets the original post URL.</summary>
    public string PostUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the attached media URL, if any.</summary>
    public string? MediaUrl { get; set; }

    /// <summary>Gets or sets the attached media kind.</summary>
    public GacSocialMediaType MediaType { get; set; } = GacSocialMediaType.None;

    /// <summary>Gets or sets the UTC timestamp the post was originally published, if known.</summary>
    public DateTimeOffset? PostedAt { get; set; }

    /// <summary>Gets or sets the optional engagement metrics.</summary>
    public SocialMetrics? Metrics { get; set; }

    /// <summary>Gets or sets the publishing account handle.</summary>
    public string Account { get; set; } = string.Empty;
}
