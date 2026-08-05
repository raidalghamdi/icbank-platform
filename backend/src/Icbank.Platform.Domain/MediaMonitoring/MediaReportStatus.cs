namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Lifecycle status of a media report (DATA-MODEL.md section 5).</summary>
public enum MediaReportStatus
{
    /// <summary>Draft, not yet published.</summary>
    Draft = 0,

    /// <summary>Published.</summary>
    Published = 1,

    /// <summary>Archived.</summary>
    Archived = 2,
}
