using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>
/// Audit log of every wizard/search query against final reports
/// (DATA-MODEL.md section 3.7 <c>reports_qa_queries</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>user_id</c> and <c>final_report_id</c> were both unenforced implied FKs in the
/// source schema (DATA-MODEL.md section 4). Both are now proper, enforced, optional foreign keys.
/// </remarks>
public sealed class ReportsQaQuery : AuditableEntity
{
    /// <summary>Gets or sets the id of the user who ran the query, if known.</summary>
    public int? UserId { get; set; }

    /// <summary>Gets or sets the user navigation property.</summary>
    public User? User { get; set; }

    /// <summary>Gets or sets a denormalized snapshot of the user's name.</summary>
    public string? UserName { get; set; }

    /// <summary>Gets or sets the query kind.</summary>
    public QaQueryType QueryType { get; set; }

    /// <summary>Gets or sets the wizard answers, populated only for <see cref="QaQueryType.Wizard"/> queries.</summary>
    public WizardAnswers? WizardAnswers { get; set; }

    /// <summary>Gets or sets the free-text search query, populated only for search-* query types.</summary>
    public string? SearchQuery { get; set; }

    /// <summary>Gets or sets the id of the related final report, if any.</summary>
    public int? FinalReportId { get; set; }

    /// <summary>Gets or sets the final-report navigation property.</summary>
    public FinalMediaReport? FinalReport { get; set; }

    /// <summary>Gets or sets a short result summary.</summary>
    public string? ResultSummary { get; set; }

    /// <summary>Gets or sets fully untyped metadata, as JSON text (DATA-MODEL.md section 6).</summary>
    public string? MetadataJson { get; set; }
}
