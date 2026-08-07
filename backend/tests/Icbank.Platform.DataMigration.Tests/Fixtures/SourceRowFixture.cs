using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Tests.Fixtures;

/// <summary>
/// Small test helper for building <see cref="SourceRow"/> fixtures that mirror realistic rows
/// from the actual Supabase schema (DATA-MODEL.md), without needing a live Postgres connection.
/// </summary>
public static class SourceRowFixture
{
    /// <summary>Builds a <see cref="SourceRow"/> from a column-name/value dictionary.</summary>
    /// <param name="values">The column values. Omit a key entirely to simulate SQL NULL/absent column.</param>
    /// <returns>The constructed fixture row.</returns>
    public static SourceRow Build(Dictionary<string, object?> values) => new(values);
}
