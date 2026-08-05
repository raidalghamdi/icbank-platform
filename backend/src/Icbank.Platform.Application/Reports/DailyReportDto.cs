namespace Icbank.Platform.Application.Reports;

/// <summary>The daily-report response shape (API-SURFACE.md §7).</summary>
/// <param name="Id">The report row id.</param>
/// <param name="ReportDate">The calendar date the report covers.</param>
/// <param name="ReportDataJson">The freeform report payload as raw JSON text.</param>
public sealed record DailyReportDto(int Id, DateOnly ReportDate, string ReportDataJson);
