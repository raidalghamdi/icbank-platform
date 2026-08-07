namespace Icbank.Platform.DataMigration.Source;

/// <summary>
/// Read-only port onto the source Postgres database. The migration tool never writes through
/// this interface — every table migrator (see <see cref="Migration.ITableMigrator"/>) writes
/// only through the destination EF Core model, never by hand-writing SQL. Implemented by
/// <see cref="NpgsqlDataSource"/> against a live connection, and
/// by an in-memory fake in tests, so every table transformer can be exercised with realistic
/// fixture rows without a Postgres server (task constraint: no Postgres available in this
/// environment; the read half is unverified against a real instance — see docs/DATA-MIGRATION.md).
/// </summary>
public interface IPostgresDataSource
{
    /// <summary>Streams every row of <paramref name="tableName"/>, ordered by its primary key ascending.</summary>
    /// <param name="tableName">The Postgres table name, e.g. <c>users</c>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An asynchronous sequence of raw rows.</returns>
    IAsyncEnumerable<SourceRow> ReadTableAsync(string tableName, CancellationToken cancellationToken);

    /// <summary>Gets the total row count of <paramref name="tableName"/>, for validation/reconciliation reports and progress estimation.</summary>
    /// <param name="tableName">The Postgres table name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The row count.</returns>
    Task<long> CountRowsAsync(string tableName, CancellationToken cancellationToken);

    /// <summary>Opens (and verifies) a read-only connection to the source database without reading any data.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes once connectivity is confirmed.</returns>
    Task VerifyConnectivityAsync(CancellationToken cancellationToken);
}
