namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>One entry of <c>final_media_reports.alerts</c> (DATA-MODEL.md section 6, report section 8b).</summary>
public sealed class AlertItem
{
    /// <summary>Gets or sets the alert text.</summary>
    public string Alert { get; set; } = string.Empty;

    /// <summary>Gets or sets the suggested response/position.</summary>
    public string SuggestedPosition { get; set; } = string.Empty;
}
