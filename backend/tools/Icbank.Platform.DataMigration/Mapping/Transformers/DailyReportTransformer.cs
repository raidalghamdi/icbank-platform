using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Mapping.Transformers;

/// <summary>
/// Pure transformer from a raw <c>daily_reports</c> row to <see cref="MappedDailyReport"/>.
/// </summary>
/// <remarks>
/// <c>report_date</c> maps to <see cref="DateOnly"/> via <see cref="SourceRowExtensions.GetDateOnly"/>.
/// <c>report_data</c> is fully freeform jsonb (external n8n payload shape varies, DATA-MODEL.md
/// section 6), so it is carried through verbatim as raw JSON text via
/// <see cref="JsonColumnReader.ReadRawJsonText"/> rather than deserialized into any typed shape —
/// the same untyped-JSON-text treatment already used for <c>activity_logs.details</c>.
/// </remarks>
public static class DailyReportTransformer
{
    /// <summary>Transforms one raw <c>daily_reports</c> row.</summary>
    /// <param name="row">The raw source row.</param>
    /// <returns>The mapped, destination-ready DTO.</returns>
    public static MappedDailyReport Transform(SourceRow row)
    {
        DateTime createdAtRaw = row.GetRawTimestamp("created_at")
            ?? throw new InvalidOperationException("daily_reports.created_at was null.");

        return new MappedDailyReport(
            SourceId: row.GetInt32("id"),
            ReportDate: row.GetDateOnly("report_date"),
            ReportDataJson: row.ReadRawJsonText("report_data", "{}"),
            CreatedAtUtc: createdAtRaw);
    }
}
