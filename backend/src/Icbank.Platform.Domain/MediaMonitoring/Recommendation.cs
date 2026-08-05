namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>One entry of <c>final_media_reports.recommendations</c> (DATA-MODEL.md section 6, report section 8a).</summary>
public sealed class Recommendation
{
    /// <summary>Gets or sets the recommendation title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the recommendation description.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the free-text priority label.</summary>
    public string Priority { get; set; } = string.Empty;

    /// <summary>Gets or sets the responsible party.</summary>
    public string Responsible { get; set; } = string.Empty;

    /// <summary>Gets or sets the tracked KPI.</summary>
    public string Kpi { get; set; } = string.Empty;

    /// <summary>Gets or sets the deadline, as free text from the source payload.</summary>
    public string Deadline { get; set; } = string.Empty;

    /// <summary>Gets or sets free-text dependency notes.</summary>
    public string Dependencies { get; set; } = string.Empty;
}
