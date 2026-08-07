using System.Globalization;

namespace Icbank.Platform.DataMigration.Mapping;

/// <summary>
/// Pure helper functions that pull typed values out of a raw <see cref="Source.SourceRow"/>.
/// Kept dependency-free and side-effect-free so every table transformer built on top of these
/// is trivially unit-testable with fixture rows (task requirement: pure, injectable
/// transformation functions).
/// </summary>
public static class SourceRowExtensions
{
    /// <summary>Reads a required non-null <see langword="int"/> column.</summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The column value as <see langword="int"/>.</returns>
    /// <exception cref="InvalidOperationException">The column is null or absent.</exception>
    public static int GetInt32(this Source.SourceRow row, string column) =>
        row[column] switch
        {
            null => throw new InvalidOperationException($"Column '{column}' was required but null."),
            int i => i,
            long l => checked((int)l),
            object other => Convert.ToInt32(other, CultureInfo.InvariantCulture),
        };

    /// <summary>Reads an optional nullable <see langword="int"/> column.</summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The column value, or <see langword="null"/>.</returns>
    public static int? GetNullableInt32(this Source.SourceRow row, string column) =>
        row[column] is null ? null : GetInt32(row, column);

    /// <summary>Reads a required non-null string column, defaulting to empty if the underlying value is empty text.</summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The column value as a string; never <see langword="null"/>.</returns>
    public static string GetString(this Source.SourceRow row, string column) =>
        row[column]?.ToString() ?? string.Empty;

    /// <summary>Reads an optional nullable string column.</summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The column value, or <see langword="null"/> if it was SQL NULL.</returns>
    public static string? GetNullableString(this Source.SourceRow row, string column) =>
        row[column]?.ToString();

    /// <summary>Reads a required non-null <see langword="bool"/> column.</summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The column value as <see langword="bool"/>.</returns>
    public static bool GetBoolean(this Source.SourceRow row, string column) =>
        row[column] switch
        {
            null => false,
            bool b => b,
            object other => Convert.ToBoolean(other, CultureInfo.InvariantCulture),
        };

    /// <summary>Reads an optional nullable <see langword="bool"/> column.</summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The column value, or <see langword="null"/>.</returns>
    public static bool? GetNullableBoolean(this Source.SourceRow row, string column) =>
        row[column] is null ? null : GetBoolean(row, column);

    /// <summary>Reads a raw, un-zoned <see cref="DateTime"/> column exactly as Postgres/Npgsql returned it (no timezone conversion applied).</summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The raw <see cref="DateTime"/> value, or <see langword="null"/> if the column was SQL NULL.</returns>
    public static DateTime? GetRawTimestamp(this Source.SourceRow row, string column) =>
        row[column] switch
        {
            null => null,
            DateTime dt => dt,
            object other => DateTime.Parse(other.ToString() ?? string.Empty, CultureInfo.InvariantCulture, DateTimeStyles.None),
        };

    /// <summary>Reads a nullable decimal column.</summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The column value as <see langword="decimal"/>, or <see langword="null"/>.</returns>
    public static decimal? GetNullableDecimal(this Source.SourceRow row, string column) =>
        row[column] is null ? null : Convert.ToDecimal(row[column], CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads a column that may arrive as a native Npgsql array, a JSON-text array (jsonb column,
    /// or an in-memory test fixture modelling the array as JSON text), or <see langword="null"/>.
    /// Shared by every transformer/migrator touching a Postgres <c>text[]</c> or
    /// <c>jsonb</c>-array column, so array-shape handling lives in one place.
    /// </summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The array elements as strings, or an empty list if the column was null/absent.</returns>
    public static IReadOnlyList<string> GetStringArray(this Source.SourceRow row, string column) =>
        row[column] switch
        {
            null => Array.Empty<string>(),
            string[] array => array,
            IEnumerable<string> enumerable => enumerable.ToArray(),
            string json when !string.IsNullOrWhiteSpace(json) =>
                System.Text.Json.JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>(),
            _ => Array.Empty<string>(),
        };

    /// <summary>
    /// Reads a column that may arrive as a native Npgsql <see langword="int"/> array, a JSON-text
    /// array (jsonb column, or an in-memory test fixture modelling the array as JSON text), or
    /// <see langword="null"/>.
    /// </summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The array elements as ints, or an empty list if the column was null/absent.</returns>
    public static IReadOnlyList<int> GetInt32Array(this Source.SourceRow row, string column) =>
        row[column] switch
        {
            null => Array.Empty<int>(),
            int[] array => array,
            IEnumerable<int> enumerable => enumerable.ToArray(),
            string json when !string.IsNullOrWhiteSpace(json) =>
                System.Text.Json.JsonSerializer.Deserialize<List<int>>(json) ?? new List<int>(),
            _ => Array.Empty<int>(),
        };

    /// <summary>Reads a nullable <see langword="float"/> column.</summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The column value as <see langword="float"/>, or <see langword="null"/>.</returns>
    public static float? GetNullableFloat(this Source.SourceRow row, string column) =>
        row[column] is null ? null : Convert.ToSingle(row[column], CultureInfo.InvariantCulture);

    /// <summary>
    /// Reads a required non-null Postgres <c>date</c> column (no time-of-day component) as a
    /// <see cref="DateOnly"/>, e.g. <c>daily_reports.report_date</c>. Modelled on
    /// <see cref="GetRawTimestamp"/>: accepts a native <see cref="DateTime"/> (the live-database
    /// shape) or a parseable ISO date string (the in-memory test-fixture shape), with no timezone
    /// conversion applied since Postgres <c>date</c> has no time-of-day or offset to begin with.
    /// </summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The column value as <see cref="DateOnly"/>.</returns>
    /// <exception cref="InvalidOperationException">The column is null or absent.</exception>
    public static DateOnly GetDateOnly(this Source.SourceRow row, string column) =>
        row[column] switch
        {
            null => throw new InvalidOperationException($"Column '{column}' was required but null."),
            DateOnly d => d,
            DateTime dt => DateOnly.FromDateTime(dt),
            object other => DateOnly.Parse(other.ToString() ?? string.Empty, CultureInfo.InvariantCulture),
        };

    /// <summary>
    /// Reads a numeric-array column (e.g. a <c>jsonb number[]</c> embedding vector) that may
    /// arrive as a native array, a JSON-text array, or <see langword="null"/>.
    /// </summary>
    /// <param name="row">The source row.</param>
    /// <param name="column">The column name.</param>
    /// <returns>The array elements as floats, or an empty list if the column was null/absent.</returns>
    public static IReadOnlyList<float> GetFloatArray(this Source.SourceRow row, string column) =>
        row[column] switch
        {
            null => Array.Empty<float>(),
            float[] array => array,
            double[] array => Array.ConvertAll(array, Convert.ToSingle),
            IEnumerable<float> enumerable => enumerable.ToArray(),
            string json when !string.IsNullOrWhiteSpace(json) =>
                System.Text.Json.JsonSerializer.Deserialize<List<float>>(json) ?? new List<float>(),
            _ => Array.Empty<float>(),
        };
}
