namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Typed shape for <c>final_media_reports.kpis</c> (DATA-MODEL.md section 6).</summary>
public sealed class ReportKpis
{
    /// <summary>Gets or sets the total news item count, if known.</summary>
    public int? TotalNews { get; set; }

    /// <summary>Gets or sets the positive-sentiment percentage, if known.</summary>
    public int? PositivePercent { get; set; }

    /// <summary>Gets or sets the distinct media outlet count, if known.</summary>
    public int? MediaOutlets { get; set; }

    /// <summary>Gets or sets the distinct key-topic count, if known.</summary>
    public int? KeyTopics { get; set; }

    /// <summary>Gets or sets the free-text reach figure, e.g. "7.2 م".</summary>
    public string? Reach { get; set; }

    /// <summary>Gets or sets the alert count, if known.</summary>
    public int? AlertsCount { get; set; }
}
