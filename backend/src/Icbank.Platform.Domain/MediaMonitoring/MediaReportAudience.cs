namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>Target audience tier for a media report (DATA-MODEL.md section 5).</summary>
public enum MediaReportAudience
{
    /// <summary>Executive audience.</summary>
    Executive = 0,

    /// <summary>Manager audience.</summary>
    Manager = 1,

    /// <summary>Analyst audience.</summary>
    Analyst = 2,

    /// <summary>Full, unabridged audience.</summary>
    Full = 3,
}
