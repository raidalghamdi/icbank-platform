namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>One entry of <c>final_media_reports.timeline</c> (DATA-MODEL.md section 6, report section 3).</summary>
public sealed class TimelineEvent
{
    /// <summary>Gets or sets the event date, as free text from the source payload.</summary>
    public string Date { get; set; } = string.Empty;

    /// <summary>Gets or sets the event description.</summary>
    public string Event { get; set; } = string.Empty;

    /// <summary>Gets or sets the reporting outlet.</summary>
    public string Outlet { get; set; } = string.Empty;

    /// <summary>Gets or sets the editorial tone label.</summary>
    public string Tone { get; set; } = string.Empty;

    /// <summary>Gets or sets the mention count for this event.</summary>
    public int Count { get; set; }
}
