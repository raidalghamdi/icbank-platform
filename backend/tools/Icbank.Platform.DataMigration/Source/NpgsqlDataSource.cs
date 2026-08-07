using System.Runtime.CompilerServices;
using Npgsql;

namespace Icbank.Platform.DataMigration.Source;

/// <summary>
/// <see cref="IPostgresDataSource"/> implementation backed by a live Npgsql connection. This is
/// the only class in the tool that talks to Postgres — everything downstream (transformers,
/// validation, writer) depends on the interface only. Opens a fresh connection per call so the
/// tool can run for hours across many tables without holding one long-lived connection open.
/// </summary>
/// <remarks>
/// Never verified against a real Supabase instance in this environment (no Postgres server or
/// network access available) — see docs/DATA-MIGRATION.md "Unverified assumptions". Column
/// names/quoting/order follow Supabase's Drizzle-generated snake_case schema (supabase/ and
/// lib/db/src/schema/*.ts).
/// </remarks>
public sealed class NpgsqlDataSource : IPostgresDataSource, IAsyncDisposable
{
    private readonly string _connectionString;

    /// <summary>Initializes a new instance of the <see cref="NpgsqlDataSource"/> class.</summary>
    /// <param name="connectionString">The Npgsql connection string, read from configuration/environment only.</param>
    public NpgsqlDataSource(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <inheritdoc />
    public async Task VerifyConnectivityAsync(CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenReadOnlyConnectionAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<long> CountRowsAsync(string tableName, CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenReadOnlyConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {QuoteIdentifier(tableName)}";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return result is null ? 0L : Convert.ToInt64(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<SourceRow> ReadTableAsync(
        string tableName,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await using NpgsqlConnection connection = await OpenReadOnlyConnectionAsync(cancellationToken);
        await using NpgsqlCommand command = connection.CreateCommand();

        // Most source tables use an integer id, but shorfah_section_sla_defaults has the
        // natural text key section_type. Ordering by the first source column keeps every table
        // deterministic without incorrectly assuming an id column exists.
        command.CommandText = $"SELECT * FROM {QuoteIdentifier(tableName)} ORDER BY 1 ASC";
        await using NpgsqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            yield return ReadRow(reader);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => ValueTask.CompletedTask;

    private static SourceRow ReadRow(NpgsqlDataReader reader)
    {
        var values = new Dictionary<string, object?>(reader.FieldCount, StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < reader.FieldCount; i++)
        {
            values[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
        }

        return new SourceRow(values);
    }

    /// <summary>Quotes a Postgres identifier defensively; table names in this tool are always from our own fixed registry, never user input.</summary>
    private static string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    private async Task<NpgsqlConnection> OpenReadOnlyConnectionAsync(CancellationToken cancellationToken)
    {
        var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        // Belt-and-braces: the tool must never write to Postgres. Setting the session itself
        // read-only means even a coding mistake in a future change cannot mutate source data.
        await using NpgsqlCommand setReadOnly = connection.CreateCommand();
        setReadOnly.CommandText = "SET SESSION CHARACTERISTICS AS TRANSACTION READ ONLY;";
        await setReadOnly.ExecuteNonQueryAsync(cancellationToken);

        return connection;
    }
}
