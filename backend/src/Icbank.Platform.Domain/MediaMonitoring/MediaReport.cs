using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>
/// AI-generated, editable media-monitoring report -- draft/published tier, not the immutable
/// "final" tier (DATA-MODEL.md section 3.7 <c>media_reports</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>generated_by_user_id</c> was an unenforced implied FK in the source schema
/// (DATA-MODEL.md section 4). It is now a proper, enforced, optional foreign key.
/// </remarks>
public sealed class MediaReport : AuditableEntity
{
    /// <summary>Gets or sets the report title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the report cadence/type.</summary>
    public MediaReportType ReportType { get; set; } = MediaReportType.Weekly;

    /// <summary>Gets or sets the target audience tier.</summary>
    public MediaReportAudience Audience { get; set; } = MediaReportAudience.Manager;

    /// <summary>Gets or sets the UTC start of the covered date range.</summary>
    public DateTimeOffset DateFrom { get; set; }

    /// <summary>Gets or sets the UTC end of the covered date range.</summary>
    public DateTimeOffset DateTo { get; set; }

    /// <summary>Gets or sets the included source list, e.g. "linkedin", "news".</summary>
    public List<string> Sources { get; set; } = new();

    /// <summary>Gets or sets the AI-generated executive summary.</summary>
    public string? ExecutiveSummary { get; set; }

    /// <summary>Gets or sets the AI-generated markdown body.</summary>
    public string ContentMd { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional computed statistics.</summary>
    public MediaReportStats? Stats { get; set; }

    /// <summary>Gets or sets the overall AI-generated tone summary.</summary>
    public string? OverallTone { get; set; }

    /// <summary>
    /// Gets or sets the raw source-row snapshot used to build this report, as JSON text.
    /// Intentionally untyped (DATA-MODEL.md section 6) since it is a heterogeneous audit snapshot.
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

    /// <summary>Gets or sets the lifecycle status.</summary>
    public MediaReportStatus Status { get; set; } = MediaReportStatus.Published;
}
