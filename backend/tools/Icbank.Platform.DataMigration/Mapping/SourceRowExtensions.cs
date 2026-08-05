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
}
