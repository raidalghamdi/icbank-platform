using System.Text.Json;

namespace Icbank.Platform.Application.Campaigns;

/// <summary>
/// Reads the campaign operating plan document. The plan is owned by the campaign page; the
/// server only relies on the pieces it has to enforce: completion of tasks and deliverables
/// (which drives progress) and the measured results (which drive the board totals).
/// </summary>
public static class CampaignPlan
{
    /// <summary>The largest plan document accepted, in characters.</summary>
    public const int MaxLength = 2_000_000;

    /// <summary>Parses a stored plan into a detached JSON element.</summary>
    /// <param name="planJson">The stored plan.</param>
    /// <returns>The plan element, or null when the stored plan is empty or unreadable.</returns>
    public static JsonElement? ToElement(string? planJson)
    {
        if (string.IsNullOrWhiteSpace(planJson))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(planJson);
            return document.RootElement.ValueKind == JsonValueKind.Object ? document.RootElement.Clone() : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Returns whether the text is a JSON object within the size limit.</summary>
    /// <param name="planJson">The plan to check.</param>
    /// <returns><see langword="true"/> when the plan can be stored.</returns>
    public static bool IsValid(string planJson)
        => planJson is not null && planJson.Length <= MaxLength && ToElement(planJson) is not null;

    /// <summary>
    /// Computes progress as the share of completed tasks and deliverables. A plan without any
    /// tracked work returns null so the caller keeps the existing figure.
    /// </summary>
    /// <param name="plan">The plan element.</param>
    /// <returns>The percentage 0-100, or null when nothing is tracked.</returns>
    public static int? Progress(JsonElement plan)
    {
        (var doneTasks, var totalTasks) = CountDone(plan, "tasks");
        (var doneItems, var totalItems) = CountDone(plan, "deliverables");
        var total = totalTasks + totalItems;
        return total == 0 ? null : (int)Math.Round((doneTasks + doneItems) * 100d / total, MidpointRounding.AwayFromZero);
    }

    /// <summary>Reads a whole-number measured result, e.g. <c>results.reach</c>.</summary>
    /// <param name="plan">The plan element.</param>
    /// <param name="metric">The metric key.</param>
    /// <returns>The value, or null when not recorded.</returns>
    public static int? Result(JsonElement plan, string metric)
    {
        if (!plan.TryGetProperty("results", out JsonElement results) || results.ValueKind != JsonValueKind.Object
            || !results.TryGetProperty(metric, out JsonElement value) || value.ValueKind != JsonValueKind.Number)
        {
            return null;
        }

        return value.TryGetInt64(out var number) ? (int)Math.Clamp(number, 0, int.MaxValue) : null;
    }

    /// <summary>Reads the deliverables list the campaign page manages, if the plan has one.</summary>
    /// <param name="plan">The plan element, or null.</param>
    /// <returns>The deliverables, or null when the plan does not manage them yet.</returns>
    public static List<CampaignDeliverableDto>? Deliverables(JsonElement? plan)
    {
        if (plan is not { } root || !root.TryGetProperty("deliverables", out JsonElement list) || list.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        return list.EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.Object)
            .Select(item => new CampaignDeliverableDto(
                Text(item, "title"),
                DateTime.TryParse(Text(item, "dueDate"), System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal, out DateTime due) ? due : default,
                item.TryGetProperty("done", out JsonElement flag) && flag.ValueKind == JsonValueKind.True))
            .ToList();
    }

    private static string Text(JsonElement item, string name)
        => item.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String ? value.GetString() ?? string.Empty : string.Empty;

    private static (int Done, int Total) CountDone(JsonElement plan, string listName)
    {
        if (!plan.TryGetProperty(listName, out JsonElement list) || list.ValueKind != JsonValueKind.Array)
        {
            return (0, 0);
        }

        var done = 0;
        var total = 0;
        foreach (JsonElement item in list.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            total++;
            if (item.TryGetProperty("done", out JsonElement flag) && flag.ValueKind == JsonValueKind.True)
            {
                done++;
            }
        }

        return (done, total);
    }
}
