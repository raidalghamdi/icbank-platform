using System.Globalization;
using System.Text;
using System.Text.Json;

namespace Icbank.Platform.DataMigration.Reporting;

/// <summary>
/// Writes a <see cref="MigrationReport"/> to disk as both machine-readable JSON and a
/// human-readable text summary (task requirement: a final report file). Never receives or
/// writes connection strings, credentials, or personal data — the report only ever contains row
/// counts, table names, and free-text structural findings (e.g. "3 duplicate keys found"), never
/// column values such as emails or names.
/// </summary>
public static class ReportWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    /// <summary>Writes the report as timestamped JSON and text files under <paramref name="reportDirectory"/>.</summary>
    /// <param name="report">The report to write.</param>
    /// <param name="reportDirectory">The output directory; created if missing.</param>
    /// <returns>The paths of the two files written.</returns>
    public static (string JsonPath, string TextPath) Write(MigrationReport report, string reportDirectory)
    {
        Directory.CreateDirectory(reportDirectory);
        var stamp = report.StartedAtUtc.UtcDateTime.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        var baseName = $"{report.Mode.ToLowerInvariant()}-{stamp}";

        var jsonPath = Path.Combine(reportDirectory, baseName + ".json");
        var textPath = Path.Combine(reportDirectory, baseName + ".txt");

        File.WriteAllText(jsonPath, JsonSerializer.Serialize(report, JsonOptions));
        File.WriteAllText(textPath, RenderText(report));

        return (jsonPath, textPath);
    }

    /// <summary>Renders a human-readable summary of the report, suitable for pasting into a cutover ticket.</summary>
    /// <param name="report">The report to render.</param>
    /// <returns>The rendered text.</returns>
    public static string RenderText(MigrationReport report)
    {
        var builder = new StringBuilder();
        builder.AppendLine(CultureInfo.InvariantCulture, $"Data migration report — mode: {report.Mode}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Started (UTC):  {report.StartedAtUtc:O}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Finished (UTC): {report.FinishedAtUtc:O}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Duration:       {report.FinishedAtUtc - report.StartedAtUtc}");
        builder.AppendLine(CultureInfo.InvariantCulture, $"Overall result: {(report.OverallPass ? "PASS" : "FAIL")}");
        builder.AppendLine();
        builder.AppendLine("Per-table results:");
        foreach (TableReportEntry table in report.Tables)
        {
            var destinationCount = table.DestinationRowCount.HasValue
                ? string.Format(CultureInfo.InvariantCulture, ", destination={0}", table.DestinationRowCount)
                : string.Empty;
            var summary = string.Format(
                CultureInfo.InvariantCulture,
                "  [{0}] {1}: source={2}{3}",
                table.Pass ? "PASS" : "FAIL",
                table.TableName,
                table.SourceRowCount,
                destinationCount);
            builder.AppendLine(summary);
            foreach (var note in table.Notes)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"      - {note}");
            }
        }

        if (report.Findings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Run-level findings:");
            foreach (var finding in report.Findings)
            {
                builder.AppendLine(CultureInfo.InvariantCulture, $"  - {finding}");
            }
        }

        return builder.ToString();
    }
}
