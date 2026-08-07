using System.Text.Json;

namespace Icbank.Platform.DataMigration.Mapping;

/// <summary>
/// Deserializes a raw <c>jsonb</c> column value into a typed .NET object, for the destination
/// entities that store the same JSON shape via an EF <c>HasConversion</c> JSON column (e.g.
/// <see cref="Icbank.Platform.Domain.Designs.DesignTemplate"/>,
/// <see cref="Icbank.Platform.Domain.MediaMonitoring.MediaReport"/>). Postgres/Drizzle column
/// values use camelCase property names; the destination C# types use PascalCase, so case-
/// insensitive matching is required.
/// </summary>
public static class JsonColumnReader
{
    private static readonly JsonSerializerOptions Options = CreateOptions();

    /// <summary>Deserializes a raw column value (JSON text, or a pre-parsed <see cref="JsonElement"/>) into <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The target .NET type.</typeparam>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The deserialized value, or <see langword="null"/> if the column was SQL NULL/absent.</returns>
    public static T? ReadObject<T>(this Source.SourceRow row, string column)
        where T : class
    {
        var raw = row[column];
        return raw switch
        {
            null => null,
            JsonElement je when je.ValueKind == JsonValueKind.Null => null,
            JsonElement je => je.Deserialize<T>(Options),
            string s when string.IsNullOrWhiteSpace(s) || s == "null" => null,
            string s => JsonSerializer.Deserialize<T>(s, Options),
            _ => JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(raw), Options),
        };
    }

    /// <summary>Deserializes a raw column value into a list of <typeparamref name="T"/>, defaulting to an empty list.</summary>
    /// <typeparam name="T">The list element type.</typeparam>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The deserialized list, or an empty list if the column was SQL NULL/absent.</returns>
    public static List<T> ReadObjectList<T>(this Source.SourceRow row, string column)
        where T : class
    {
        var raw = row[column];
        List<T>? result = raw switch
        {
            null => null,
            JsonElement je when je.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined => null,
            JsonElement je => je.Deserialize<List<T>>(Options),
            string s when string.IsNullOrWhiteSpace(s) || s == "null" => null,
            string s => JsonSerializer.Deserialize<List<T>>(s, Options),
            _ => JsonSerializer.Deserialize<List<T>>(JsonSerializer.Serialize(raw), Options),
        };

        return result ?? new List<T>();
    }

    /// <summary>Reads a raw <c>jsonb</c> column and returns its canonical JSON text verbatim (re-serialized), for destination columns that store the payload as untyped JSON text.</summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <param name="fallback">The text to return if the column was SQL NULL/absent.</param>
    /// <returns>The JSON text.</returns>
    public static string ReadRawJsonText(this Source.SourceRow row, string column, string fallback)
    {
        var raw = row[column];
        return raw switch
        {
            null => fallback,
            JsonElement je when je.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined => fallback,
            JsonElement je => je.GetRawText(),
            string s when string.IsNullOrWhiteSpace(s) || s == "null" => fallback,
            string s => s,
            _ => JsonSerializer.Serialize(raw),
        };
    }

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        };
        options.Converters.Add(new NumberToStringConverter());
        options.Converters.Add(new NullToZeroIntConverter());
        return options;
    }

    private sealed class NumberToStringConverter : System.Text.Json.Serialization.JsonConverter<string>
    {
        public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.Number => ReadNumber(ref reader),
                JsonTokenType.True => bool.TrueString.ToLowerInvariant(),
                JsonTokenType.False => bool.FalseString.ToLowerInvariant(),
                _ => throw new JsonException($"Cannot convert JSON token {reader.TokenType} to string."),
            };

        public override void Write(Utf8JsonWriter writer, string value, JsonSerializerOptions options) =>
            writer.WriteStringValue(value);

        private static string ReadNumber(ref Utf8JsonReader reader)
        {
            if (reader.TryGetInt64(out var integer))
            {
                return integer.ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            return reader.GetDouble().ToString("R", System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    private sealed class NullToZeroIntConverter : System.Text.Json.Serialization.JsonConverter<int>
    {
        public override bool HandleNull => true;

        public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
            reader.TokenType switch
            {
                JsonTokenType.Null => 0,
                JsonTokenType.Number => reader.GetInt32(),
                _ => throw new JsonException($"Cannot convert JSON token {reader.TokenType} to int."),
            };

        public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options) =>
            writer.WriteNumberValue(value);
    }
}
