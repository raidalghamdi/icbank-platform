using Icbank.Platform.DataMigration.Migration;
using Icbank.Platform.DataMigration.Reporting;
using Icbank.Platform.DataMigration.Source;
using Microsoft.Extensions.Logging;

namespace Icbank.Platform.DataMigration.Validation;

/// <summary>
/// Pre-flight, read-only validation mode (task requirement 4): connects to the source read-only,
/// reports row counts per table, and estimates duration. Never opens a write transaction and
/// never touches the destination.
/// </summary>
public sealed partial class ValidationRunner
{
    private const int EstimatedRowsPerSecond = 200;

    private readonly IPostgresDataSource _source;
    private readonly ILogger _logger;

    /// <summary>Initializes a new instance of the <see cref="ValidationRunner"/> class.</summary>
    /// <param name="source">The read-only Postgres source.</param>
    /// <param name="logger">The structured logger.</param>
    public ValidationRunner(IPostgresDataSource source, ILogger logger)
    {
        _source = source;
        _logger = logger;
    }

    /// <summary>Runs the validation pass over every table in <see cref="TableMigratorRegistry"/>.</summary>
    /// <param name="startedAtUtc">The instant this run started, for the report header.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The completed validation report. Contains no writes, ever.</returns>
    public async Task<MigrationReport> RunAsync(DateTimeOffset startedAtUtc, CancellationToken cancellationToken)
    {
        var report = new MigrationReport { Mode = "Validate", StartedAtUtc = startedAtUtc };

        LogVerifyingConnectivity(_logger);
        await _source.VerifyConnectivityAsync(cancellationToken);

        long totalRows = 0;
        foreach (ITableMigrator migrator in TableMigratorRegistry.GetOrderedMigrators())
        {
            var rowCount = await _source.CountRowsAsync(migrator.SourceTableName, cancellationToken);
            totalRows += rowCount;

            var entry = new TableReportEntry { TableName = migrator.SourceTableName, SourceRowCount = rowCount };
            report.Tables.Add(entry);
            LogTableRowCount(_logger, migrator.SourceTableName, rowCount);
        }

        var estimatedSeconds = totalRows / (double)EstimatedRowsPerSecond;
        report.AddFinding($"Total source rows across {report.Tables.Count} registered table(s): {totalRows}.");
        report.AddFinding($"Estimated migration duration at ~{EstimatedRowsPerSecond} rows/sec: {TimeSpan.FromSeconds(estimatedSeconds):g}.");
        report.AddFinding(
            "This estimate and every row count above come only from the tables registered in " +
            "TableMigratorRegistry today; see spec/DATA-MIGRATION-NOTES.md for the full 44-table " +
            "status including tables not yet covered by a dedicated migrator.");

        report.FinishedAtUtc = DateTimeOffset.UtcNow;
        return report;
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Validation: {Table} has {RowCount} source row(s).")]
    private static partial void LogTableRowCount(ILogger logger, string table, long rowCount);

    [LoggerMessage(Level = LogLevel.Information, Message = "Validation: verifying read-only connectivity to source.")]
    private static partial void LogVerifyingConnectivity(ILogger logger);
}
