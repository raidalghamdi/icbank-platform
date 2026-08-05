namespace Icbank.Platform.DataMigration.Migration;

/// <summary>
/// Migrates one source table into its destination entity type, in an idempotent, resumable way.
/// Implementations own the read → transform → resolve-FKs → write → record-id-mapping pipeline
/// for exactly one table, so the FK-safe dependency order (task requirement 2) is expressed by
/// the order <see cref="ITableMigrator"/> instances are invoked in, not by anything inside a
/// single migrator.
/// </summary>
public interface ITableMigrator
{
    /// <summary>Gets the source Postgres table name this migrator reads from.</summary>
    string SourceTableName { get; }

    /// <summary>Gets the destination table name this migrator writes to, for reporting.</summary>
    string DestinationTableName { get; }

    /// <summary>
    /// Runs the migration for this table: reads every source row, skips rows already recorded in
    /// the id-mapping store (idempotency), transforms and writes the rest, and records a new
    /// mapping entry for each newly-written row.
    /// </summary>
    /// <param name="context">The shared migration run context (source, destination, id-map, clock, reporter).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A summary of what happened for this table.</returns>
    Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken);

    /// <summary>
    /// Counts rows currently in the destination table (including soft-deleted rows, so
    /// reconciliation is not skewed by the soft-delete query filter), for reconciliation mode.
    /// </summary>
    /// <param name="context">The shared migration run context.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The destination row count.</returns>
    Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken);
}
