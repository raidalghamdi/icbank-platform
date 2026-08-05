using System.Text.Json;
using System.Text.Json.Nodes;

namespace Icbank.Platform.Application.Reports;

/// <summary>
/// Ports the Node <c>normalizeN8NPayload()</c> field-remapping rule verbatim (BUSINESS-RULES.md
/// §6): remaps <c>overdue_projects→overdueProjects</c>, <c>due_soon_projects→dueSoon</c>,
/// <c>target_initiatives→initiatives</c>; passes <c>kpis</c>/<c>breakdowns</c> through only if
/// they are JSON objects; stamps <c>_receivedAt</c>/<c>_source:"n8n"</c> for provenance; deletes
/// the original <c>report_date</c>/<c>reportDate</c> keys from the stored payload.
/// </summary>
public static class N8NPayloadNormalizer
{
    private const string Source = "n8n";

    /// <summary>Normalizes a raw n8n JSON payload into the internal storage shape.</summary>
    /// <param name="rawJson">The raw, untrusted JSON payload text.</param>
    /// <param name="receivedAtUtc">The UTC instant the payload was received, stamped as provenance.</param>
    /// <returns>The normalized JSON payload text, with <c>report_date</c>/<c>reportDate</c> removed.</returns>
    public static string Normalize(string rawJson, DateTimeOffset receivedAtUtc)
    {
        JsonNode root = JsonNode.Parse(rawJson) ?? new JsonObject();
        JsonObject reportData = root.AsObject();

        RemapIfPresent(reportData, "overdue_projects", "overdueProjects");
        RemapIfPresent(reportData, "due_soon_projects", "dueSoon");
        RemapIfPresent(reportData, "target_initiatives", "initiatives");
        PassThroughObjectOnly(reportData, "kpis");
        PassThroughObjectOnly(reportData, "breakdowns");

        reportData["_receivedAt"] = receivedAtUtc.ToString("O", System.Globalization.CultureInfo.InvariantCulture);
        reportData["_source"] = Source;

        reportData.Remove("report_date");
        reportData.Remove("reportDate");

        return reportData.ToJsonString();
    }

    /// <summary>Extracts the report date from either <c>report_date</c> or <c>reportDate</c>, preferring the former (matches Node's <c>||</c> precedence).</summary>
    /// <param name="rawJson">The raw, untrusted JSON payload text.</param>
    /// <returns>The extracted date string, or <c>null</c> if neither key is present.</returns>
    public static string? ExtractReportDate(string rawJson)
    {
        JsonNode root = JsonNode.Parse(rawJson) ?? new JsonObject();
        JsonObject reportData = root.AsObject();
        return reportData["report_date"]?.GetValue<string>() ?? reportData["reportDate"]?.GetValue<string>();
    }

    private static void RemapIfPresent(JsonObject reportData, string sourceKey, string targetKey)
    {
        if (reportData.TryGetPropertyValue(sourceKey, out JsonNode? value) && value is not null)
        {
            reportData[targetKey] = value.DeepClone();
        }
    }

    private static void PassThroughObjectOnly(JsonObject reportData, string key)
    {
        if (reportData.TryGetPropertyValue(key, out JsonNode? value) && value is JsonObject)
        {
            reportData[key] = value.DeepClone();
        }
    }
}
