namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>
/// Status of a final, immutable media report (DATA-MODEL.md section 5). The source system
/// always sets <see cref="Final"/> -- there is no other value in practice; it exists purely as
/// an immutability marker. See DOMAIN-PORT-NOTES.md for the immutability-enforcement decision.
/// </summary>
public enum FinalMediaReportStatus
{
    /// <summary>The report is final and immutable.</summary>
    Final = 0,

    /// <summary>Generated and still in the Review &amp; Edit / Preview stages; editable until approved.</summary>
    Draft = 1,
}
