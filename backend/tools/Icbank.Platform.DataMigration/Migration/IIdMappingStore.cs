namespace Icbank.Platform.DataMigration.Migration;

/// <summary>
/// Port onto the source-id → destination-id mapping table that makes the migration idempotent
/// (task requirement 2). See <see cref="IdMappingStore"/> for the production implementation and
/// its design rationale for staying outside the EF Core model.
/// </summary>
public interface IIdMappingStore
{
    /// <summary>Creates the mapping table if it does not already exist.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task EnsureCreatedAsync(CancellationToken cancellationToken);

    /// <summary>Looks up a previously recorded destination id for a source row, if this row was already migrated.</summary>
    /// <param name="sourceTable">The Postgres source table name.</param>
    /// <param name="sourceId">The Postgres row's surrogate key.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The destination id, or <see langword="null"/> if this row has not been migrated yet.</returns>
    Task<int?> TryGetDestinationIdAsync(string sourceTable, int sourceId, CancellationToken cancellationToken);

    /// <summary>Records that a source row has been migrated to a given destination id.</summary>
    /// <param name="sourceTable">The Postgres source table name.</param>
    /// <param name="sourceId">The Postgres row's surrogate key.</param>
    /// <param name="destinationId">The newly assigned SQL Server surrogate key.</param>
    /// <param name="migratedAt">The timestamp the row was migrated at.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the operation.</returns>
    Task RecordAsync(string sourceTable, int sourceId, int destinationId, DateTimeOffset migratedAt, CancellationToken cancellationToken);
}
