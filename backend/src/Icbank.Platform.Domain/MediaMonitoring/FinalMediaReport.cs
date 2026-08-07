using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>
/// Immutable, officially-numbered 8-section GAC media report -- no update/delete by design
/// (DATA-MODEL.md section 3.7 <c>final_media_reports</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>generated_by_user_id</c> was an unenforced implied FK in the source schema
/// (DATA-MODEL.md section 4). It is now a proper, enforced, optional foreign key. This entity
/// intentionally has no soft-delete: immutability is the design intent, so soft-delete would be
/// the wrong pattern (matches DATA-MODEL.md section 8's own observation for this table).
/// </remarks>
public sealed class FinalMediaReport : AuditableEntity
{
    /// <summary>Gets or sets the official report number, e.g. "GAC-MEDIA-21/2026".</summary>
    public string ReportNumber { get; set; } = string.Empty;

    /// <summary>Gets or sets the report title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the report cadence/type.</summary>
    public MediaReportType ReportType { get; set; } = MediaReportType.Weekly;

    /// <summary>Gets or sets the human-readable period label, e.g. "مايو 2026".</summary>
    public string PeriodLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC start of the covered date range.</summary>
    public DateTimeOffset DateFrom { get; set; }

    /// <summary>Gets or sets the UTC end of the covered date range.</summary>
    public DateTimeOffset DateTo { get; set; }

    /// <summary>Gets or sets the preparing department.</summary>
    public string? PreparedBy { get; set; } = "الإدارة التنفيذية للتواصل المؤسسي";

    /// <summary>Gets or sets the report beneficiary.</summary>
    public string? Beneficiary { get; set; } = "الإدارة التنفيذية";

    /// <summary>Gets or sets the internal reference number.</summary>
    public string? ReferenceNumber { get; set; }

    /// <summary>Gets or sets the classification label.</summary>
    public string? Classification { get; set; } = "سري — للاستخدام الداخلي";

    /// <summary>Gets or sets the UTC issue date.</summary>
    public DateTimeOffset IssueDate { get; set; }

    /// <summary>Gets or sets the report's key performance indicators.</summary>
    public ReportKpis Kpis { get; set; } = new();

    /// <summary>Gets or sets the executive summary (section 1).</summary>
    public string? ExecutiveSummary { get; set; }

    /// <summary>Gets or sets the top news items (section 2).</summary>
    public List<TopNewsItem> TopNews { get; set; } = new();

    /// <summary>Gets or sets the detailed timeline (section 3).</summary>
    public List<TimelineEvent> Timeline { get; set; } = new();

    /// <summary>Gets or sets the digital-presence analysis (section 4).</summary>
    public DigitalPresence DigitalPresence { get; set; } = new();

    /// <summary>Gets or sets the editorial-tone analysis (section 5).</summary>
    public EditorialTone EditorialTone { get; set; } = new();

    /// <summary>Gets or sets the deep sectoral analysis (section 6).</summary>
    public DeepAnalysis DeepAnalysis { get; set; } = new();

    /// <summary>Gets or sets the regional comparison table (section 7).</summary>
    public List<RegionalComparison> RegionalComparison { get; set; } = new();

    /// <summary>Gets or sets the recommendations and action plan (section 8a).</summary>
    public List<Recommendation> Recommendations { get; set; } = new();

    /// <summary>Gets or sets the alerts and suggested positions (section 8b).</summary>
    public List<AlertItem> Alerts { get; set; } = new();

    /// <summary>Gets or sets the quotes appendix.</summary>
    public List<QuoteAppendixItem> QuotesAppendix { get; set; } = new();

    /// <summary>Gets or sets the methodology notes.</summary>
    public string? Methodology { get; set; }

    /// <summary>Gets or sets the source list.</summary>
    public List<SourceRef> Sources { get; set; } = new();

    /// <summary>
    /// Gets or sets the raw source-row snapshot used to build this report, as JSON text.
    /// Intentionally untyped (DATA-MODEL.md section 6).
    /// </summary>
    public string SourceItemsJson { get; set; } = "[]";

    /// <summary>Gets or sets the id of the user who generated this report, if known.</summary>
    public int? GeneratedByUserId { get; set; }

    /// <summary>Gets or sets the generating-user navigation property.</summary>
    public User? GeneratedByUser { get; set; }

    /// <summary>Gets or sets a denormalized snapshot of the generating user's name.</summary>
    public string? GeneratedByName { get; set; }

    /// <summary>Gets or sets the AI model used to generate the report.</summary>
    public string? AiModel { get; set; } = "gemini-2.5-flash";

    /// <summary>Gets or sets the status, always <see cref="FinalMediaReportStatus.Final"/> in practice.</summary>
    public FinalMediaReportStatus Status { get; set; } = FinalMediaReportStatus.Final;

    /// <summary>Gets or sets the UTC timestamp the report was locked.</summary>
    public DateTimeOffset LockedAt { get; set; }

    /// <summary>Gets or sets the SHA-256 integrity fingerprint of the JSON payload.</summary>
    public string ContentSha256 { get; set; } = string.Empty;

    /// <summary>Gets or sets the storage key of the rendered PDF, if any.</summary>
    public string? PdfStorageKey { get; set; }

    /// <summary>Gets or sets the view counter.</summary>
    public int ViewCount { get; set; }

    /// <summary>Gets the QA/search queries recorded against this report.</summary>
    public ICollection<ReportsQaQuery> QaQueries { get; init; } = new List<ReportsQaQuery>();
}
