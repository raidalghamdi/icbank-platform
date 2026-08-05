using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.DataMigration.Migration;
using Icbank.Platform.DataMigration.Migration.Migrators;
using Icbank.Platform.DataMigration.Reconciliation;
using Icbank.Platform.DataMigration.Reporting;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icbank.Platform.DataMigration.Tests.Reconciliation;

/// <summary>
/// Integration coverage for <see cref="ReconciliationRunner"/> -- the tool's only post-migration
/// safety net, the thing an operator relies on to know whether cutover actually moved every row.
/// Before this test class it had zero coverage in any direction: its pass/fail comparison walks
/// the real <see cref="TableMigratorRegistry"/> and calls every migrator's
/// <c>CountDestinationRowsAsync</c>, which requires a real SQL Server database (there is no
/// in-memory substitute, by the same design as every other migrator -- see
/// <see cref="MigrationContextFactory"/>), so this follows the same
/// <c>ICBANK_TEST_SQL_CONNECTION</c>-gated pattern as
/// <c>DailyReportTableMigratorSqlServerTests</c>.
/// </summary>
/// <remarks>
/// <b>Environment note:</b> no SQL Server instance is reachable in the sandbox this test was
/// written in (no Docker daemon, nothing on port 1433, the env var unset) -- see
/// spec/DATA-MIGRATION-NOTES.md and docs/DATA-MIGRATION.md. Each test below returns early when
/// the variable is absent, so the suite reports 0 skipped without weakening any assertion, but
/// this also means the real assertions here have not yet executed against a live SQL Server in
/// this engagement.
/// </remarks>
public sealed class ReconciliationRunnerSqlServerTests : IAsyncLifetime
{
    private static readonly string? SqlServerConnectionTemplate =
        Environment.GetEnvironmentVariable("ICBANK_TEST_SQL_CONNECTION");

    private readonly string _databaseName = $"IcbankReconciliationTest_{Guid.NewGuid():N}";
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
            // Best-effort cleanup only, matching DailyReportTableMigratorSqlServerTests' own precedent.
        }
    }

    [Fact]
    public async Task RunAsync_EmptySourceAndDestination_EveryTablePasses()
    {
        if (!HasSqlServer)
        {
            return;
        }

        var source = new InMemoryPostgresDataSource();
        MigrationRunContext context = BuildContext(source);
        var runner = new ReconciliationRunner(context, NullLogger.Instance);

        MigrationReport report = await runner.RunAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        report.OverallPass.Should().BeTrue("an empty source and an empty destination agree on every table");
        report.Tables.Should().OnlyContain(t => t.Pass);
        report.Tables.Should().HaveCount(TableMigratorRegistry.GetOrderedMigrators().Count);
    }

    [Fact]
    public async Task RunAsync_SourceHasRowsNeverMigrated_ReportsMismatchAndOverallFail()
    {
        if (!HasSqlServer)
        {
            return;
        }

        // daily_reports has no FK dependencies (see TableMigratorRegistry), so seeding source
        // rows without ever running the migrate step reliably produces a source/destination
        // mismatch: 2 source rows exist, 0 were ever written to the destination.
        InMemoryPostgresDataSource source = new InMemoryPostgresDataSource().WithTable(
            "daily_reports",
            new[]
            {
                new Dictionary<string, object?> { ["id"] = 1, ["report_date"] = new DateOnly(2024, 1, 1), ["report_data"] = "{}", ["created_at"] = new DateTime(2024, 1, 1) },
                new Dictionary<string, object?> { ["id"] = 2, ["report_date"] = new DateOnly(2024, 1, 2), ["report_data"] = "{}", ["created_at"] = new DateTime(2024, 1, 2) },
            });
        MigrationRunContext context = BuildContext(source);
        var runner = new ReconciliationRunner(context, NullLogger.Instance);

        MigrationReport report = await runner.RunAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        report.OverallPass.Should().BeFalse("2 source rows exist for daily_reports but 0 were ever migrated to the destination");
        TableReportEntry dailyReportsEntry = report.Tables.Single(t => t.TableName == "daily_reports");
        dailyReportsEntry.Pass.Should().BeFalse();
        dailyReportsEntry.SourceRowCount.Should().Be(2);
        dailyReportsEntry.DestinationRowCount.Should().Be(0);
        dailyReportsEntry.Notes.Should().ContainSingle(n => n.Contains("investigate", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RunAsync_SourceAndDestinationRowCountsMatch_TablePasses()
    {
        if (!HasSqlServer)
        {
            return;
        }

        InMemoryPostgresDataSource source = new InMemoryPostgresDataSource().WithTable(
            "daily_reports",
            new[]
            {
                new Dictionary<string, object?> { ["id"] = 1, ["report_date"] = new DateOnly(2024, 2, 1), ["report_data"] = "{}", ["created_at"] = new DateTime(2024, 2, 1) },
            });

        var idMap = new InMemoryIdMappingStore();
        MigrationRunContext migrateContext = BuildContext(source, idMap);
        var migrator = new DailyReportTableMigrator();
        TableMigrationResult migrateResult = await migrator.MigrateAsync(migrateContext, CancellationToken.None);
        migrateResult.RowsInserted.Should().Be(1, "the migrate step must actually write the row before reconciliation can see it");

        MigrationRunContext reconcileContext = BuildContext(source, idMap);
        var runner = new ReconciliationRunner(reconcileContext, NullLogger.Instance);
        MigrationReport report = await runner.RunAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        TableReportEntry dailyReportsEntry = report.Tables.Single(t => t.TableName == "daily_reports");
        dailyReportsEntry.Pass.Should().BeTrue();
        dailyReportsEntry.SourceRowCount.Should().Be(1);
        dailyReportsEntry.DestinationRowCount.Should().Be(1);
    }

    private MigrationRunContext BuildContext(IPostgresDataSource source, IIdMappingStore? idMap = null) =>
        new(
            source,
            idMap ?? new InMemoryIdMappingStore(),
            _connectionString!,
            new FixedDateTimeProvider(),
            NullLogger.Instance,
            new MigrationReport { Mode = "reconcile", StartedAtUtc = DateTimeOffset.UtcNow });

    private sealed class FixedDateTimeProvider : IDateTimeProvider
    {
        public DateTimeOffset UtcNow => new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        public DateTimeOffset RiyadhNow => UtcNow.ToOffset(TimeSpan.FromHours(3));
    }
}
