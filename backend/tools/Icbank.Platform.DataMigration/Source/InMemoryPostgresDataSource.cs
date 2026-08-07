using System.Runtime.CompilerServices;

namespace Icbank.Platform.DataMigration.Source;

/// <summary>
/// In-memory <see cref="IPostgresDataSource"/> fake used by unit and integration tests to
/// exercise table transformers and the writer without a live Postgres server (task constraint:
/// no Postgres access in this environment). Production code (<see cref="NpgsqlDataSource"/>) is
/// the only implementation that ever talks to a real database.
/// </summary>
public sealed class InMemoryPostgresDataSource : IPostgresDataSource
{
    private readonly Dictionary<string, List<SourceRow>> _tables = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>Seeds a table with fixture rows for a test scenario.</summary>
    /// <param name="tableName">The table name to seed.</param>
    /// <param name="rows">The fixture rows, in the order they should be read back.</param>
    /// <returns>This instance, for chaining.</returns>
    public InMemoryPostgresDataSource WithTable(string tableName, IEnumerable<Dictionary<string, object?>> rows)
    {
        _tables[tableName] = rows.Select(r => new SourceRow(r)).ToList();
        return this;
    }

    /// <inheritdoc />
    public Task VerifyConnectivityAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<long> CountRowsAsync(string tableName, CancellationToken cancellationToken) =>
        Task.FromResult(_tables.TryGetValue(tableName, out List<SourceRow>? rows) ? rows.Count : 0L);

    /// <inheritdoc />
    public async IAsyncEnumerable<SourceRow> ReadTableAsync(
        string tableName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (!_tables.TryGetValue(tableName, out List<SourceRow>? rows))
        {
            yield break;
        }

        foreach (SourceRow row in rows)
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return row;
            await Task.Yield();
        }
    }
}
