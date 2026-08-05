namespace Icbank.Platform.DataMigration.Migration;

/// <summary>
/// In-memory <see cref="IIdMappingStore"/> fake for unit and integration tests, so idempotency
/// behavior (re-run after partial failure does not duplicate rows) can be verified without a
/// live SQL Server connection for the store itself.
/// </summary>
public sealed class InMemoryIdMappingStore : IIdMappingStore
{
    private readonly Dictionary<(string Table, int SourceId), int> _map = new();

    /// <inheritdoc />
    public Task EnsureCreatedAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<int?> TryGetDestinationIdAsync(string sourceTable, int sourceId, CancellationToken cancellationToken) =>
        Task.FromResult(_map.TryGetValue((sourceTable, sourceId), out int destinationId) ? destinationId : (int?)null);

    /// <inheritdoc />
    public Task RecordAsync(string sourceTable, int sourceId, int destinationId, DateTimeOffset migratedAt, CancellationToken cancellationToken)
    {
        _map[(sourceTable, sourceId)] = destinationId;
        return Task.CompletedTask;
    }
}
