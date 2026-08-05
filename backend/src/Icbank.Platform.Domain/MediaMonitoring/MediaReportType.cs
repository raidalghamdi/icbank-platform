namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Report cadence/scope (DATA-MODEL.md section 5).</summary>
public enum MediaReportType
{
    /// <summary>A weekly report.</summary>
    Weekly = 0,

    /// <summary>A monthly report.</summary>
    Monthly = 1,

    /// <summary>A custom date-range report.</summary>
    Custom = 2,

    /// <summary>An ad-hoc, one-off report.</summary>
    Adhoc = 3,
}
