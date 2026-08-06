using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.DataMigration.Migration;
using Icbank.Platform.DataMigration.Migration.Migrators;
using Icbank.Platform.DataMigration.Reporting;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Migration;

/// <summary>
/// Integration coverage for <see cref="GacNewsItemTableMigrator"/> writes against the real SQL
/// Server harness used in CI.
/// </summary>
public sealed class GacNewsItemTableMigratorSqlServerTests : IAsyncLifetime
{
    private static readonly string? SqlServerConnectionTemplate =
        Environment.GetEnvironmentVariable("ICBANK_TEST_SQL_CONNECTION");

    private readonly string _databaseName = $"IcbankGacNewsMigrationTest_{Guid.NewGuid():N}";
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
            // Best-effort cleanup only, matching the established SQL Server integration tests.
        }
    }

    [Fact]
    public async Task MigrateAsync_ObservedProductionCategories_InsertsThemWithoutDataRejections()
    {
        if (!HasSqlServer)
        {
            return;
        }

        var source = new FakeSource(new[]
        {
            Row(1, "regulation"),
            Row(2, "press_release"),
        });

        TableMigrationResult result = await new GacNewsItemTableMigrator()
            .MigrateAsync(BuildContext(source), CancellationToken.None);

        result.RowsRead.Should().Be(2);
        result.RowsInserted.Should().Be(2);
        result.RowsSkippedDueToDataIssue.Should().Be(0);

        await using var verify = new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(_connectionString).Options);
        List<GacNewsItem> rows = await verify.GacNewsItems.OrderBy(item => item.TitleAr).ToListAsync();
        rows.Select(item => item.Category).Should().BeEquivalentTo(new GacNewsCategory?[]
        {
            GacNewsCategory.Regulation,
            GacNewsCategory.PressRelease,
        });
    }

    private static SourceRow Row(int id, string category) =>
        new(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["kind"] = "news",
            ["title_ar"] = $"خبر {id}",
            ["category"] = category,
            ["tags"] = Array.Empty<string>(),
            ["created_at"] = new DateTime(2024, 1, id),
        });

    private MigrationRunContext BuildContext(IPostgresDataSource source) =>
        new(
            source,
            new InMemoryIdMappingStore(),
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

        public async IAsyncEnumerable<SourceRow> ReadTableAsync(
            string tableName,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            foreach (SourceRow row in _rows)
            {
                yield return row;
                await Task.Yield();
            }
        }

        public Task<long> CountRowsAsync(string tableName, CancellationToken cancellationToken) =>
            Task.FromResult((long)_rows.Count);

        public Task VerifyConnectivityAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
