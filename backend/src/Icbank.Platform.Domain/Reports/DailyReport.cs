using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Reports;

/// <summary>
/// Structured daily-report payload ingested from an n8n automation
/// (DATA-MODEL.md section 3.3 <c>daily_reports</c>).
/// </summary>
public sealed class DailyReport : AuditableEntity
{
    /// <summary>Gets or sets the calendar date the report covers.</summary>
    public DateOnly ReportDate { get; set; }

    /// <summary>
    /// Gets or sets the fully freeform report payload as raw JSON text. The external n8n
    /// payload shape varies, so this is intentionally not modeled as a rigid DTO
    /// (DATA-MODEL.md section 6).
    /// </summary>
    public string ReportDataJson { get; set; } = "{}";
}
