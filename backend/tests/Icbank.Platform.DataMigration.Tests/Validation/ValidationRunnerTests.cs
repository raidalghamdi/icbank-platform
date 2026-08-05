using FluentAssertions;
using Icbank.Platform.DataMigration.Migration;
using Icbank.Platform.DataMigration.Reporting;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Validation;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icbank.Platform.DataMigration.Tests.Validation;

/// <summary>
/// <see cref="ValidationRunner"/> is the tool's only pre-flight, read-only safety check -- the
/// one mode an operator is expected to run against the real source before ever opening a write
/// transaction. Before this test class it had zero coverage: a defect here (wrong row counts,
/// a silently-swallowed connectivity failure, a broken total) would only surface once someone
/// was already relying on its numbers to size a downtime window or sign off on a cutover.
/// </summary>
public sealed class ValidationRunnerTests
{
    [Fact]
    public async Task RunAsync_VerifiesConnectivityBeforeReadingAnyTable()
    {
        var source = new RecordingPostgresDataSource();
        var runner = new ValidationRunner(source, NullLogger.Instance);

        await runner.RunAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        source.ConnectivityVerified.Should().BeTrue("validate mode must never read a table without first confirming the source is reachable");
    }

    [Fact]
    public async Task RunAsync_ReportsOneEntryPerRegisteredTable_WithSourceRowCounts()
    {
        var source = new RecordingPostgresDataSource();
        source.WithTable("roles", 3).WithTable("users", 10);
        var runner = new ValidationRunner(source, NullLogger.Instance);

        MigrationReport report = await runner.RunAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        // TableMigratorRegistry currently has 42 registered migrators; validate mode must produce
        // exactly one report row per registered table, not per table this test happens to seed.
        report.Tables.Should().HaveCount(TableMigratorRegistry.GetOrderedMigrators().Count);
        report.Tables.Single(t => t.TableName == "roles").SourceRowCount.Should().Be(3);
        report.Tables.Single(t => t.TableName == "users").SourceRowCount.Should().Be(10);
    }

    [Fact]
    public async Task RunAsync_UnseededTables_ReportZeroRowsRatherThanThrowing()
    {
        var source = new RecordingPostgresDataSource();
        var runner = new ValidationRunner(source, NullLogger.Instance);

        MigrationReport report = await runner.RunAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        report.Tables.Should().OnlyContain(t => t.SourceRowCount == 0);
    }

    [Fact]
    public async Task RunAsync_TotalRowsFinding_SumsAcrossAllTables()
    {
        var source = new RecordingPostgresDataSource();
        source.WithTable("roles", 5).WithTable("users", 7);
        var runner = new ValidationRunner(source, NullLogger.Instance);

        MigrationReport report = await runner.RunAsync(DateTimeOffset.UtcNow, CancellationToken.None);

        var expectedTotal = 12;
        report.Findings.Should().Contain(f => f.Contains($": {expectedTotal}.", StringComparison.Ordinal));
    }

    [Fact]
    public async Task RunAsync_SetsStartedAndFinishedTimestamps()
    {
        var source = new RecordingPostgresDataSource();
        var runner = new ValidationRunner(source, NullLogger.Instance);
        var startedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        MigrationReport report = await runner.RunAsync(startedAt, CancellationToken.None);

        report.Mode.Should().Be("Validate");
        report.StartedAtUtc.Should().Be(startedAt);
        report.FinishedAtUtc.Should().BeOnOrAfter(startedAt);
    }

    /// <summary>
    /// A fake that records whether connectivity was verified and lets each table's row count be
    /// set independently of <see cref="InMemoryPostgresDataSource"/>'s row-content seeding, since
    /// validate mode only ever calls <see cref="IPostgresDataSource.CountRowsAsync"/>, never
    /// <see cref="IPostgresDataSource.ReadTableAsync"/>.
    /// </summary>
    private sealed class RecordingPostgresDataSource : IPostgresDataSource
    {
        private readonly Dictionary<string, long> _counts = new(StringComparer.OrdinalIgnoreCase);

        public bool ConnectivityVerified { get; private set; }

        public RecordingPostgresDataSource WithTable(string tableName, long rowCount)
        {
            _counts[tableName] = rowCount;
            return this;
        }

        public Task VerifyConnectivityAsync(CancellationToken cancellationToken)
        {
            ConnectivityVerified = true;
            return Task.CompletedTask;
        }

        public Task<long> CountRowsAsync(string tableName, CancellationToken cancellationToken) =>
            Task.FromResult(_counts.TryGetValue(tableName, out var count) ? count : 0L);

#pragma warning disable CS1998 // intentionally synchronous fake; interface requires async enumerable shape
        public async IAsyncEnumerable<SourceRow> ReadTableAsync(
            string tableName,
            [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
        {
            yield break;
        }
#pragma warning restore CS1998
    }
}
