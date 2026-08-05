namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for an immutable, officially-numbered final media report (<c>final_media_reports</c>).</summary>
/// <param name="Id">The report id.</param>
/// <param name="ReportNumber">The official report number, e.g. "GAC-MEDIA-21/2026".</param>
/// <param name="Title">The report title.</param>
/// <param name="ReportType">The report cadence/type.</param>
/// <param name="PeriodLabel">The human-readable period label.</param>
/// <param name="DateFrom">The UTC start of the covered date range.</param>
/// <param name="DateTo">The UTC end of the covered date range.</param>
/// <param name="ExecutiveSummary">The executive summary (section 1).</param>
/// <param name="Kpis">The report's key performance indicators.</param>
/// <param name="Status">The lifecycle status, always <c>Final</c>.</param>
/// <param name="ViewCount">The view counter.</param>
/// <param name="ContentSha256">The SHA-256 integrity fingerprint of the JSON payload.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
public sealed record FinalMediaReportDto(
    int Id,
    string ReportNumber,
    string Title,
    string ReportType,
    string PeriodLabel,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    string? ExecutiveSummary,
    ReportKpisDto Kpis,
    string Status,
    int ViewCount,
    string ContentSha256,
    DateTimeOffset CreatedAt);
