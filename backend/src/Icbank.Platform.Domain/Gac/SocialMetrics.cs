namespace Icbank.Platform.Domain.Gac;

/// <summary>Typed shape for <c>gac_social_posts.metrics</c> (DATA-MODEL.md section 6).</summary>
public sealed class SocialMetrics
{
    /// <summary>Gets or sets the like count, if known.</summary>
    public int? Likes { get; set; }

    /// <summary>Gets or sets the comment count, if known.</summary>
    public int? Comments { get; set; }

    /// <summary>Gets or sets the share count, if known.</summary>
    public int? Shares { get; set; }
}
