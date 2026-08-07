namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Typed shape for <c>final_media_reports.digital_presence</c> (DATA-MODEL.md section 6, report section 4).</summary>
public sealed class DigitalPresence
{
    /// <summary>Gets or sets the per-platform mention/engagement breakdown.</summary>
    public List<DigitalPresencePlatform> Platforms { get; set; } = new();

    /// <summary>Gets or sets the trending hashtag list.</summary>
    public List<DigitalPresenceHashtag> Hashtags { get; set; } = new();
}

/// <summary>One platform entry nested under <see cref="DigitalPresence"/>.</summary>
public sealed class DigitalPresencePlatform
{
    /// <summary>Gets or sets the platform name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the mention count.</summary>
    public int Mentions { get; set; }

    /// <summary>Gets or sets the repost/share count.</summary>
    public int Reposts { get; set; }

    /// <summary>Gets or sets the engagement count.</summary>
    public int Engagement { get; set; }

    /// <summary>Gets or sets the free-text reach figure.</summary>
    public string Reach { get; set; } = string.Empty;
}

/// <summary>One hashtag entry nested under <see cref="DigitalPresence"/>.</summary>
public sealed class DigitalPresenceHashtag
{
    /// <summary>Gets or sets the hashtag text.</summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>Gets or sets the usage count.</summary>
    public int Uses { get; set; }

    /// <summary>Gets or sets the free-text trend descriptor.</summary>
    public string Trend { get; set; } = string.Empty;
}
