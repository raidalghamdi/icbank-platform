namespace Icbank.Platform.DataMigration.Mapping;

/// <summary>
/// Parses the source schema's <c>snake_case</c> free-text "enum" columns (DATA-MODEL.md §5 notes
/// the Postgres schema has no native enum types — every status/type column is free text enforced
/// only in application code) into the corresponding destination <c>PascalCase</c> C# enum member.
/// Pure function, unit-tested against every literal value enumerated in DATA-MODEL.md for each
/// column this is used on. Some source columns (e.g. media-monitoring's <c>category</c> and
/// <c>query_type</c>) use hyphen-delimited values such as <c>content-creation</c> instead of
/// underscores; both delimiters are treated identically.
/// </summary>
public static class SnakeCaseEnumParser
{
    /// <summary>Parses a <c>snake_case</c> source value into a destination enum of type <typeparamref name="TEnum"/>.</summary>
    /// <typeparam name="TEnum">The destination enum type.</typeparam>
    /// <param name="snakeCaseValue">The raw source value, e.g. <c>pending_contribution</c>.</param>
    /// <returns>The parsed enum value.</returns>
    /// <exception cref="ArgumentException">The value does not correspond to any member of <typeparamref name="TEnum"/>.</exception>
    public static TEnum Parse<TEnum>(string snakeCaseValue)
        where TEnum : struct, Enum
    {
        if (TryParse(snakeCaseValue, out TEnum parsed))
        {
            return parsed;
        }

        throw new ArgumentException(
            $"Value '{snakeCaseValue}' (converted to '{ToPascalCase(snakeCaseValue)}') is not a member of {typeof(TEnum).Name}.",
            nameof(snakeCaseValue));
    }

    /// <summary>
    /// Attempts to parse a <c>snake_case</c> source value into a destination enum without
    /// throwing, so a migrator can turn an unsupported source value into a counted rejection.
    /// </summary>
    /// <typeparam name="TEnum">The destination enum type.</typeparam>
    /// <param name="snakeCaseValue">The raw source value.</param>
    /// <param name="parsed">The parsed destination value when successful.</param>
    /// <returns><see langword="true"/> when the value is a member of <typeparamref name="TEnum"/>.</returns>
    public static bool TryParse<TEnum>(string snakeCaseValue, out TEnum parsed)
        where TEnum : struct, Enum =>
        Enum.TryParse(ToPascalCase(snakeCaseValue), ignoreCase: true, out parsed);

    /// <summary>Converts a <c>snake_case</c> string to <c>PascalCase</c>, e.g. <c>intl_participation</c> → <c>IntlParticipation</c>.</summary>
    /// <param name="snakeCaseValue">The source value.</param>
    /// <returns>The PascalCase equivalent.</returns>
    public static string ToPascalCase(string snakeCaseValue)
    {
        if (string.IsNullOrEmpty(snakeCaseValue))
        {
            return string.Empty;
        }

        var parts = snakeCaseValue.Split(['_', '-'], StringSplitOptions.RemoveEmptyEntries);
        return string.Concat(parts.Select(Capitalize));
    }

    private static string Capitalize(string part) =>
        part.Length == 0 ? part : char.ToUpperInvariant(part[0]) + part[1..].ToLowerInvariant();
}
