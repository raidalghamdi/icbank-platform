namespace Icbank.Platform.DataMigration.Source;

/// <summary>
/// One raw row read from a Postgres table, as an ordered column-name → value map. Values are
/// whatever the ADO.NET provider returned (<see langword="null"/> for SQL NULL, boxed CLR
/// primitives, or provider-native types like arrays). Kept deliberately untyped at this layer
/// so table transformers — not the reader — own all interpretation/casting, which is what makes
/// the transformers unit-testable without a live Postgres connection (task requirement: pure,
/// injectable transformation functions behind an interface for the data source).
/// </summary>
public sealed class SourceRow
{
    private readonly Dictionary<string, object?> _values;

    /// <summary>Initializes a new instance of the <see cref="SourceRow"/> class.</summary>
    /// <param name="values">The column-name to value map for this row.</param>
    public SourceRow(Dictionary<string, object?> values)
    {
        _values = values;
    }

    /// <summary>Gets the set of column names present on this row.</summary>
    public IReadOnlyCollection<string> ColumnNames => _values.Keys;

    /// <summary>Gets the raw value for a column, or <see langword="null"/> if the column is SQL NULL or absent.</summary>
    /// <param name="column">The source column name.</param>
    /// <returns>The raw value, or <see langword="null"/>.</returns>
    public object? this[string column] => _values.TryGetValue(column, out var value) ? value : null;
}
