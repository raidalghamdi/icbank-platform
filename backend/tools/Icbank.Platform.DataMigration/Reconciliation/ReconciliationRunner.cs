using Icbank.Platform.DataMigration.Migration;
using Icbank.Platform.DataMigration.Reporting;
using Icbank.Platform.DataMigration.Source;
using Microsoft.Extensions.Logging;

namespace Icbank.Platform.DataMigration.Reconciliation;

/// <summary>
/// Post-migration reconciliation mode (task requirement 5): compares per-table source and
/// destination row counts and produces a clear pass/fail summary. Read-only on both sides.
/// </summary>
/// <remarks>
/// Row-count parity is intentionally the pass/fail bar rather than a byte-for-byte checksum: rows
/// skipped for a documented data-quality reason (duplicate <c>gac_social_posts</c> keys, orphaned
/// FKs) make source and destination counts legitimately differ. Every such table also reports the
/// skip reason inline so a human reviewing "FAIL" rows can immediately see whether the gap is
/// expected.
/// </remarks>
public sealed partial class ReconciliationRunner
{
    private readonly MigrationRunContext _context;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ReconciliationRunner"/> class.</summary>
    /// <param name="context">The shared migration run context.</param>
    /// <param name="logger">The structured logger.</param>
    public ReconciliationRunner(MigrationRunContext context, ILogger logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>Runs the reconciliation pass over every table in <see cref="TableMigratorRegistry"/>.</summary>
    /// <param name="startedAtUtc">The instant this run started, for the report header.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completed reconciliation report.</returns>
    public async Task<MigrationReport> RunAsync(DateTimeOffset startedAtUtc, CancellationToken cancellationToken)
    {
        var report = new MigrationReport { Mode = "Reconcile", StartedAtUtc = startedAtUtc };

        foreach (ITableMigrator migrator in TableMigratorRegistry.GetOrderedMigrators())
        {
            var sourceCount = await _context.Source.CountRowsAsync(migrator.SourceTableName, cancellationToken);
            var destinationCount = await migrator.CountDestinationRowsAsync(_context, cancellationToken);

            var matches = sourceCount == destinationCount;
            var entry = new TableReportEntry
            {
                TableName = migrator.SourceTableName,
                SourceRowCount = sourceCount,
                DestinationRowCount = destinationCount,
                Pass = matches,
            };

            if (!matches)
            {
                entry.Notes.Add(
                    $"Source has {sourceCount} row(s), destination has {destinationCount}. If this table " +
                    "has known duplicate-key or orphaned-FK skips (see the migration run's own report), " +
                    "this gap may be expected -- otherwise investigate before declaring cutover complete.");
                report.OverallPass = false;
            }

            report.Tables.Add(entry);
            LogTableReconciled(_logger, migrator.SourceTableName, sourceCount, destinationCount, matches);
        }

        report.FinishedAtUtc = DateTimeOffset.UtcNow;
        return report;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Reconciliation: {Table} source={SourceCount} destination={DestinationCount} pass={Pass}.")]
    private static partial void LogTableReconciled(ILogger logger, string table, long sourceCount, long destinationCount, bool pass);
}
