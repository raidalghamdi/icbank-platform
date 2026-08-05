namespace Icbank.Platform.DataMigration.Mapping.Dtos;

/// <summary>Pure DTO produced by <see cref="Transformers.DailyReportTransformer"/>.</summary>
/// <param name="SourceId">The source Postgres <c>daily_reports.id</c>.</param>
/// <param name="ReportDate">
/// The calendar date the report covers (part of the new unique index the destination adds —
/// DATA-MODEL.md section 3.3 flags <c>report_date</c> as only an "implied UNIQUE" in the source,
/// with no actual database constraint, so duplicate source rows for the same date are possible).
/// </param>
/// <param name="ReportDataJson">The fully freeform report payload, carried through as raw JSON text.</param>
/// <param name="CreatedAtUtc">The original row-creation instant.</param>
public sealed record MappedDailyReport(
    int SourceId,
    DateOnly ReportDate,
    string ReportDataJson,
    DateTime CreatedAtUtc);
