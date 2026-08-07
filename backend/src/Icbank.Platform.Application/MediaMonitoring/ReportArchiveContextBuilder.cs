using System.Text;
using System.Text.Json;
using Icbank.Platform.Domain.MediaMonitoring;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Builds the AI Q&amp;A context block for up to 5 matched final reports (BUSINESS-RULES.md
/// §5.5): concatenates each report's <c>executiveSummary</c>, <c>topNews</c> (truncated to 1500
/// chars), <c>recommendations</c> (800 chars), <c>deepAnalysis</c> (800 chars), and
/// <c>quotesAppendix</c> (800 chars). These truncation lengths are magic numbers carried over
/// exactly from the Node source, chosen to fit within the model's context/token budget, not
/// derived from any documented constraint.
/// </summary>
public static class ReportArchiveContextBuilder
{
    private const int TopNewsTruncateLength = 1500;
    private const int RecommendationsTruncateLength = 800;
    private const int DeepAnalysisTruncateLength = 800;
    private const int QuotesAppendixTruncateLength = 800;

    /// <summary>Builds the context block for the given matched reports.</summary>
    /// <param name="reports">The matched final reports, in relevance order.</param>
    /// <returns>The concatenated context block.</returns>
    public static string Build(IReadOnlyList<FinalMediaReport> reports)
    {
        var builder = new StringBuilder();
        foreach (FinalMediaReport report in reports)
        {
            AppendReportSection(builder, report);
        }

        return builder.ToString();
    }

    private static void AppendReportSection(StringBuilder builder, FinalMediaReport report)
    {
        builder.Append("تقرير رقم ").Append(report.ReportNumber).Append(":\n");
        builder.Append(report.ExecutiveSummary ?? string.Empty).Append('\n');
        builder.Append(Truncate(JsonSerializer.Serialize(report.TopNews), TopNewsTruncateLength)).Append('\n');
        builder.Append(Truncate(JsonSerializer.Serialize(report.Recommendations), RecommendationsTruncateLength)).Append('\n');
        builder.Append(Truncate(JsonSerializer.Serialize(report.DeepAnalysis), DeepAnalysisTruncateLength)).Append('\n');
        builder.Append(Truncate(JsonSerializer.Serialize(report.QuotesAppendix), QuotesAppendixTruncateLength)).Append("\n\n");
    }

    private static string Truncate(string value, int maxLength) => value.Length <= maxLength ? value : value[..maxLength];
}
