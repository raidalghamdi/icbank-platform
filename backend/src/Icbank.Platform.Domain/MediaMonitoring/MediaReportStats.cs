namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Typed shape for <c>media_reports.stats</c> (DATA-MODEL.md section 6).</summary>
public sealed class MediaReportStats
{
    /// <summary>Gets or sets the total post count, if known.</summary>
    public int? TotalPosts { get; set; }

    /// <summary>Gets or sets the LinkedIn post count, if known.</summary>
    public int? LinkedinCount { get; set; }

    /// <summary>Gets or sets the news item count, if known.</summary>
    public int? NewsCount { get; set; }

    /// <summary>Gets or sets the tone distribution, keyed by tone label.</summary>
    public Dictionary<string, int> ToneDistribution { get; set; } = new();

    /// <summary>Gets or sets the top recurring themes.</summary>
    public List<string> TopThemes { get; set; } = new();
}
