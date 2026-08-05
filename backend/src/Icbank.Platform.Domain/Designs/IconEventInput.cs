namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// The fully-resolved input to the icon-event HTML renderer, after AI extraction and all
/// code-enforced post-processing rules (BUSINESS-RULES.md §7.4) have been applied.
/// </summary>
public sealed class IconEventInput
{
    /// <summary>Gets or sets the main headline (2-6 words).</summary>
    public string Headline { get; set; } = string.Empty;

    /// <summary>Gets or sets the subtitle body text.</summary>
    public string? Subtitle { get; set; }

    /// <summary>Gets or sets the department name, empty when not present in the source input.</summary>
    public string? Department { get; set; }

    /// <summary>Gets or sets the hashtag, empty when not present in the source input.</summary>
    public string? Hashtag { get; set; }

    /// <summary>Gets or sets the literal contact email extracted from the raw input.</summary>
    public string? ContactEmail { get; set; }

    /// <summary>Gets or sets the literal contact phone extracted from the raw input.</summary>
    public string? ContactPhone { get; set; }

    /// <summary>Gets or sets the event date string.</summary>
    public string? Date { get; set; }

    /// <summary>Gets or sets the event time string.</summary>
    public string? Time { get; set; }

    /// <summary>Gets or sets the event location string.</summary>
    public string? Location { get; set; }

    /// <summary>Gets or sets the main icon name.</summary>
    public string MainIcon { get; set; } = string.Empty;

    /// <summary>Gets or sets the supporting icon names (0-3).</summary>
    public List<string> SupportingIcons { get; set; } = new();

    /// <summary>Gets or sets the statistic chips, only populated for layouts that render them.</summary>
    public List<IconEventStat> Stats { get; set; } = new();

    /// <summary>Gets or sets the layout variant.</summary>
    public IconEventLayoutType Layout { get; set; }

    /// <summary>Gets or sets the size preset.</summary>
    public IconEventSizePreset Size { get; set; }

    /// <summary>Gets or sets the logo URL, if any.</summary>
    public string? LogoUrl { get; set; }
}
