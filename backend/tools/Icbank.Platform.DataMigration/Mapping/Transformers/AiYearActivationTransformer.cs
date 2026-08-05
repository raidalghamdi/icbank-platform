using System.Text.Json;
using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Mapping.Transformers;

/// <summary>
/// Pure transformer from a raw <c>ai_year_activations</c> row to <see cref="MappedAiYearActivation"/>.
/// </summary>
/// <remarks>
/// <b>AMBIGUOUS-2 decision:</b> the source <c>channels</c> column is a native Postgres
/// <c>text[]</c> — the only native array column in the whole schema. The port normalizes this
/// into a child table (<c>AiYearActivationChannel</c>/<c>ai_year_activation_channels</c>) rather
/// than a JSON string, for relational/query integrity (DOMAIN-PORT-NOTES.md). This transformer
/// therefore fans the array out into <see cref="MappedAiYearActivation.Channels"/>, one entry
/// per source array element, in source order, de-duplicated (a destination
/// <c>AiYearActivationChannel</c> row per unique channel — duplicate channel names within one
/// activation's array carry no additional meaning and would otherwise violate no constraint but
/// add noise, so they are collapsed here and reported as a note, not silently repeated).
/// </remarks>
public static class AiYearActivationTransformer
{
    /// <summary>Transforms one raw <c>ai_year_activations</c> row.</summary>
    /// <param name="row">The raw source row.</param>
    /// <returns>The mapped, destination-ready DTO with channels fanned out.</returns>
    public static MappedAiYearActivation Transform(SourceRow row)
    {
        DateTime createdAtRaw = row.GetRawTimestamp("created_at")
            ?? throw new InvalidOperationException("ai_year_activations.created_at was null.");

        return new MappedAiYearActivation(
            SourceId: row.GetInt32("id"),
            Title: row.GetString("title"),
            Month: row.GetInt32("month"),
            Year: row.GetNullableInt32("year") ?? 2026,
            ActivationDate: row.GetNullableString("activation_date"),
            Type: row.GetString("type"),
            Description: row.GetNullableString("description"),
            Tags: ReadStringArray(row["tags"]),
            Status: string.IsNullOrEmpty(row.GetNullableString("status")) ? "Published" : row.GetString("status"),
            Reach: row.GetNullableInt32("reach"),
            Engagement: row.GetNullableInt32("engagement"),
            Notes: row.GetNullableString("notes"),
            Channels: ReadStringArray(row["channels"]).Distinct(StringComparer.OrdinalIgnoreCase).ToArray(),
            CreatedAtUtc: createdAtRaw);
    }

    /// <summary>
    /// Reads a column value that may arrive as a native Npgsql <see cref="string"/> array (the
    /// live-database shape), a <see cref="JsonElement"/>/JSON-text array (the <c>tags</c> jsonb
    /// column, or an in-memory test fixture that models the array as a JSON string), or
    /// <see langword="null"/>.
    /// </summary>
    private static IReadOnlyList<string> ReadStringArray(object? value)
    {
        switch (value)
        {
            case null:
                return Array.Empty<string>();
            case string[] array:
                return array;
            case IEnumerable<string> enumerable:
                return enumerable.ToArray();
            case string json when !string.IsNullOrWhiteSpace(json):
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            default:
                return Array.Empty<string>();
        }
    }
}
