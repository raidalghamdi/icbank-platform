using Icbank.Platform.DataMigration.Reporting;
using Microsoft.Extensions.Logging;

namespace Icbank.Platform.DataMigration.Migration;

/// <summary>
/// Drives migrate mode: runs every <see cref="ITableMigrator"/> in
/// <see cref="TableMigratorRegistry"/>'s FK-safe order and assembles the final report.
/// </summary>
public sealed partial class MigrationOrchestrator
{
    private readonly MigrationRunContext _context;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="MigrationOrchestrator"/> class.</summary>
    /// <param name="context">The shared migration run context.</param>
    /// <param name="logger">The structured logger.</param>
    public MigrationOrchestrator(MigrationRunContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Runs every registered table migrator in order.</summary>
    /// <param name="startedAtUtc">The instant this run started, for the report header.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completed migration report.</returns>
    public async Task<MigrationReport> RunAsync(DateTimeOffset startedAtUtc, CancellationToken cancellationToken)
    {
        var report = new MigrationReport { Mode = "Migrate", StartedAtUtc = startedAtUtc };

        await _context.IdMap.EnsureCreatedAsync(cancellationToken);

        foreach (ITableMigrator migrator in TableMigratorRegistry.GetOrderedMigrators())
        {
            LogStartingTable(_logger, migrator.SourceTableName);

            TableMigrationResult result = await migrator.MigrateAsync(_context, cancellationToken);

            var entry = new TableReportEntry
            {
                TableName = result.SourceTableName,
                SourceRowCount = result.RowsRead,
                DestinationRowCount = result.RowsInserted + result.RowsSkippedAlreadyMigrated,
                Pass = result.RowsSkippedDueToDataIssue == 0,
            };
            entry.Notes.AddRange(result.Notes);
            if (result.RowsSkippedDueToDataIssue > 0)
            {
                entry.Notes.Add($"{result.RowsSkippedDueToDataIssue} row(s) skipped due to a data-quality issue -- see notes above.");
                report.OverallPass = false;
            }

            report.Tables.Add(entry);

            LogFinishedTable(
                _logger,
                migrator.SourceTableName,
                result.Duration,
                result.RowsRead,
                result.RowsInserted,
                result.RowsSkippedAlreadyMigrated,
                result.RowsSkippedDueToDataIssue);
        }

        report.FinishedAtUtc = DateTimeOffset.UtcNow;
        return report;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Migrate: starting {Table}.")]
    private static partial void LogStartingTable(ILogger logger, string table);

    [LoggerMessage(Level = LogLevel.Information, Message = "Migrate: finished {Table} in {Duration} -- read={Read} inserted={Inserted} alreadyMigrated={AlreadyMigrated} dataIssue={DataIssue}.")]
    private static partial void LogFinishedTable(ILogger logger, string table, TimeSpan duration, int read, int inserted, int alreadyMigrated, int dataIssue);
}
