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
        string stamp = report.StartedAtUtc.UtcDateTime.ToString("yyyyMMdd-HHmmss", System.Globalization.CultureInfo.InvariantCulture);
        string baseName = $"{report.Mode.ToLowerInvariant()}-{stamp}";

        string jsonPath = Path.Combine(reportDirectory, baseName + ".json");
        string textPath = Path.Combine(reportDirectory, baseName + ".txt");

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
        builder.AppendLine($"Data migration report — mode: {report.Mode}");
        builder.AppendLine($"Started (UTC):  {report.StartedAtUtc:O}");
        builder.AppendLine($"Finished (UTC): {report.FinishedAtUtc:O}");
        builder.AppendLine($"Duration:       {report.FinishedAtUtc - report.StartedAtUtc}");
        builder.AppendLine($"Overall result: {(report.OverallPass ? "PASS" : "FAIL")}");
        builder.AppendLine();
        builder.AppendLine("Per-table results:");
        foreach (TableReportEntry table in report.Tables)
        {
            builder.AppendLine($"  [{(table.Pass ? "PASS" : "FAIL")}] {table.TableName}: source={table.SourceRowCount}"
                + (table.DestinationRowCount.HasValue ? $", destination={table.DestinationRowCount}" : string.Empty));
            foreach (string note in table.Notes)
            {
                builder.AppendLine($"      - {note}");
            }
        }

        if (report.Findings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Run-level findings:");
            foreach (string finding in report.Findings)
            {
                builder.AppendLine($"  - {finding}");
            }
        }

        return builder.ToString();
    }
}
