using Microsoft.Data.SqlClient;

namespace Icbank.Platform.DataMigration.Migration;

/// <summary>
/// Tracks the mapping from a source Postgres surrogate key to the destination SQL Server
/// surrogate key, so re-running the tool after a partial failure is idempotent (task requirement
/// 2) instead of re-inserting already-migrated rows under new ids.
/// </summary>
/// <remarks>
/// <para><b>Design decision (see docs/DATA-MIGRATION.md and spec/DATA-MIGRATION-NOTES.md):</b>
/// this table (<c>_migration_id_map</c>) is deliberately <b>not</b> modeled as an EF Core entity
/// in <c>AppDbContext</c>. It is created and managed with plain ADO.NET
/// (<see cref="Microsoft.Data.SqlClient"/>) by this class alone. Reasons: (1) it is bookkeeping
/// for this tool only, never queried by the running API, and has no business meaning in the
/// Domain layer; (2) keeping it out of the EF model means gate 6
/// (<c>dotnet ef migrations has-pending-model-changes</c>) stays clean with zero risk of drift
/// from this tool's existence — the main application's model is completely untouched by this
/// project. The table is created idempotently (<c>IF NOT EXISTS</c>) on first use.</para>
/// </remarks>
public sealed class IdMappingStore : IIdMappingStore, IAsyncDisposable
{
    private const string TableName = "_migration_id_map";
    private readonly string _connectionString;
    private SqlConnection? _connection;

    /// <summary>Initializes a new instance of the <see cref="IdMappingStore"/> class.</summary>
    /// <param name="connectionString">The destination SQL Server connection string.</param>
    public IdMappingStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task EnsureCreatedAsync(CancellationToken cancellationToken)
    {
        SqlConnection connection = await GetConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = '{TableName}')
            BEGIN
                CREATE TABLE {TableName} (
                    source_table nvarchar(100) NOT NULL,
                    source_id int NOT NULL,
                    destination_id int NOT NULL,
                    migrated_at datetime2(3) NOT NULL,
                    CONSTRAINT pk_migration_id_map PRIMARY KEY (source_table, source_id)
                );
            END
            """;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<int?> TryGetDestinationIdAsync(string sourceTable, int sourceId, CancellationToken cancellationToken)
    {
        SqlConnection connection = await GetConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT destination_id FROM {TableName} WHERE source_table = @sourceTable AND source_id = @sourceId";
        command.Parameters.AddWithValue("@sourceTable", sourceTable);
        command.Parameters.AddWithValue("@sourceId", sourceId);

        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null or DBNull ? null : Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public async Task RecordAsync(string sourceTable, int sourceId, int destinationId, DateTimeOffset migratedAt, CancellationToken cancellationToken)
    {
        SqlConnection connection = await GetConnectionAsync(cancellationToken);
        await using SqlCommand command = connection.CreateCommand();
        command.CommandText = $"""
            INSERT INTO {TableName} (source_table, source_id, destination_id, migrated_at)
            VALUES (@sourceTable, @sourceId, @destinationId, @migratedAt)
            """;
        command.Parameters.AddWithValue("@sourceTable", sourceTable);
        command.Parameters.AddWithValue("@sourceId", sourceId);
        command.Parameters.AddWithValue("@destinationId", destinationId);
        command.Parameters.AddWithValue("@migratedAt", migratedAt.UtcDateTime);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }
    }

    private async Task<SqlConnection> GetConnectionAsync(CancellationToken cancellationToken)
    {
        if (_connection is null)
        {
            _connection = new SqlConnection(_connectionString);
            await _connection.OpenAsync(cancellationToken);
        }

        return _connection;
    }
}
