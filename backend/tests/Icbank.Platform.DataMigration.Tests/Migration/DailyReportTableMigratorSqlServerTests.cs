using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.DataMigration.Migration;
using Icbank.Platform.DataMigration.Migration.Migrators;
using Icbank.Platform.DataMigration.Reporting;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Migration;

/// <summary>
/// Integration coverage for <see cref="DailyReportTableMigrator"/>'s EF write half against a
/// real SQL Server database, using the exact same
/// <c>ICBANK_TEST_SQL_CONNECTION</c>-gated pattern as
/// <c>Icbank.Platform.IntegrationTests.Auth.AuthWebApplicationFactory</c> (task requirement 4:
/// integration-test the write half against the real SQL Server harness the rest of the suite
/// already uses).
/// </summary>
/// <remarks>
/// <b>Environment note (see docs/DATA-MIGRATION.md and spec/DATA-MIGRATION-NOTES.md):</b> no
/// SQL Server instance is reachable in the sandbox this engagement was carried out in — no
/// Docker daemon, no local SQL Server, and <c>ICBANK_TEST_SQL_CONNECTION</c> is unset. Every test
/// below therefore skips itself early (by returning, since xunit v2 has no attribute-free
/// runtime skip) when the variable is absent, so the
/// suite stays green locally without silently deleting or weakening any assertion. When this
/// project runs in CI with a real SQL Server (the same environment the IntegrationTests
/// project already depends on), these tests execute for
/// real and are the first actual proof the write half behaves as designed; until then, the write
/// half must be treated as reviewed-by-inspection only, not proven.
/// </remarks>
public sealed class DailyReportTableMigratorSqlServerTests : IAsyncLifetime
{
    private static readonly string? SqlServerConnectionTemplate =
        Environment.GetEnvironmentVariable("ICBANK_TEST_SQL_CONNECTION");

    private readonly string _databaseName = $"IcbankDataMigrationTest_{Guid.NewGuid():N}";
    private string? _connectionString;

    private static bool HasSqlServer => !string.IsNullOrWhiteSpace(SqlServerConnectionTemplate);

    /// <inheritdoc />
    public async Task InitializeAsync()
    {
        if (!HasSqlServer)
        {
            return;
        }

        _connectionString = new SqlConnectionStringBuilder(SqlServerConnectionTemplate)
        {
            InitialCatalog = _databaseName,
        }.ConnectionString;

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using var context = new AppDbContext(options);
        await context.Database.MigrateAsync();
    }

    /// <inheritdoc />
    public async Task DisposeAsync()
    {
        if (!HasSqlServer || _connectionString is null)
        {
            return;
        }

        SqlConnection.ClearAllPools();
        var masterBuilder = new SqlConnectionStringBuilder(SqlServerConnectionTemplate) { InitialCatalog = "master" };
        try
        {
            await using var connection = new SqlConnection(masterBuilder.ConnectionString);
            await connection.OpenAsync();
            await using SqlCommand command = connection.CreateCommand();
            command.CommandText =
                $"IF DB_ID(N'{_databaseName}') IS NOT NULL BEGIN " +
                $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; " +
                $"DROP DATABASE [{_databaseName}]; END";
            await command.ExecuteNonQueryAsync();
        }
        catch (SqlException)
        {
            // Best-effort cleanup only, matching AuthWebApplicationFactory's own precedent.
        }
    }

    [Fact]
    public async Task MigrateAsync_TwoRows_InsertsBothWithCorrectFields()
    {
        if (!HasSqlServer)
        {
            return;
        }

        var source = new FakeSource(new[]
        {
            FakeSource.Row(1, new DateOnly(2024, 3, 15), "{\"summary\":\"a\"}", new DateTime(2024, 3, 15, 8, 0, 0)),
            FakeSource.Row(2, new DateOnly(2024, 3, 16), "{\"summary\":\"b\"}", new DateTime(2024, 3, 16, 8, 0, 0)),
        });

        MigrationRunContext runContext = BuildContext(source);
        var migrator = new DailyReportTableMigrator();

        TableMigrationResult result = await migrator.MigrateAsync(runContext, CancellationToken.None);

        result.RowsInserted.Should().Be(2);

        await using var verify = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options);
        List<Icbank.Platform.Domain.Reports.DailyReport> rows = await verify.DailyReports.OrderBy(r => r.ReportDate).ToListAsync();
        rows.Should().HaveCount(2);
        rows[0].ReportDate.Should().Be(new DateOnly(2024, 3, 15));
        rows[0].ReportDataJson.Should().Be("{\"summary\":\"a\"}");
        rows[1].ReportDate.Should().Be(new DateOnly(2024, 3, 16));
    }

    [Fact]
    public async Task MigrateAsync_DuplicateReportDate_KeepsEarliestAndSkipsRest()
    {
        if (!HasSqlServer)
        {
            return;
        }

        var source = new FakeSource(new[]
        {
            FakeSource.Row(1, new DateOnly(2024, 4, 1), "{\"v\":1}", new DateTime(2024, 4, 1, 9, 0, 0)),
            FakeSource.Row(2, new DateOnly(2024, 4, 1), "{\"v\":2}", new DateTime(2024, 4, 1, 8, 0, 0)),
        });

        MigrationRunContext runContext = BuildContext(source);
        var migrator = new DailyReportTableMigrator();

        TableMigrationResult result = await migrator.MigrateAsync(runContext, CancellationToken.None);

        result.RowsInserted.Should().Be(1);
        result.RowsSkippedDueToDataIssue.Should().Be(1);

        await using var verify = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options);
        List<Icbank.Platform.Domain.Reports.DailyReport> rows = await verify.DailyReports.ToListAsync();
        rows.Should().ContainSingle();
        rows[0].ReportDataJson.Should().Be("{\"v\":2}", "the earliest-created row (08:00) must win over the later one (09:00)");
    }

    [Fact]
    public async Task MigrateAsync_RunTwice_IsIdempotent()
    {
        if (!HasSqlServer)
        {
            return;
        }

        var source = new FakeSource(new[]
        {
            FakeSource.Row(1, new DateOnly(2024, 5, 1), "{}", new DateTime(2024, 5, 1, 8, 0, 0)),
        });

        var idMap = new InMemoryIdMappingStore();
        var migrator = new DailyReportTableMigrator();

        await migrator.MigrateAsync(BuildContext(source, idMap), CancellationToken.None);
        TableMigrationResult second = await migrator.MigrateAsync(BuildContext(source, idMap), CancellationToken.None);

        second.RowsInserted.Should().Be(0);
        second.RowsSkippedAlreadyMigrated.Should().Be(1);

        await using var verify = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options);
        var count = await verify.DailyReports.CountAsync();
        count.Should().Be(1, "re-running the migrator must never duplicate a row already recorded in the id map");
    }

    private MigrationRunContext BuildContext(IPostgresDataSource source, IIdMappingStore? idMap = null) =>
        new(
            source,
            idMap ?? new InMemoryIdMappingStore(),
            _connectionString!,
            new FixedDateTimeProvider(),
            NullLogger.Instance,
            new MigrationReport { Mode = "migrate", StartedAtUtc = DateTimeOffset.UtcNow });

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public DateTimeOffset RiyadhNow => UtcNow.ToOffset(TimeSpan.FromHours(3));
    }

    private sealed class FakeSource : IPostgresDataSource
    {
        private readonly IReadOnlyList<SourceRow> _rows;

        public FakeSource(IReadOnlyList<SourceRow> rows) => _rows = rows;

        public static SourceRow Row(int id, DateOnly reportDate, string reportDataJson, DateTime createdAt) =>
            new(new Dictionary<string, object?>
            {
                ["id"] = id,
                ["report_date"] = reportDate,
                ["report_data"] = reportDataJson,
                ["created_at"] = createdAt,
            });

        public async IAsyncEnumerable<SourceRow> ReadTableAsync(string tableName, [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (SourceRow row in _rows)
            {
                yield return row;
                await Task.Yield();
            }
        }

        public Task<long> CountRowsAsync(string tableName, CancellationToken cancellationToken) => Task.FromResult((long)_rows.Count);

        public Task VerifyConnectivityAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
