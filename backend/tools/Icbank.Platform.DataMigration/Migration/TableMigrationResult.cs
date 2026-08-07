namespace Icbank.Platform.DataMigration.Migration;

/// <summary>Outcome of migrating one table, for the final report.</summary>
public sealed class TableMigrationResult
{
    /// <summary>Gets or sets the source table name.</summary>
    public string SourceTableName { get; set; } = string.Empty;

    /// <summary>Gets or sets the number of rows read from the source.</summary>
    public int RowsRead { get; set; }

    /// <summary>Gets or sets the number of rows newly inserted this run.</summary>
    public int RowsInserted { get; set; }

    /// <summary>Gets or sets the number of rows skipped because they were already migrated in a previous run (idempotency).</summary>
    public int RowsSkippedAlreadyMigrated { get; set; }

    /// <summary>Gets or sets the number of rows skipped because they failed a data-quality check (e.g. a duplicate key) and were reported instead of written.</summary>
    public int RowsSkippedDueToDataIssue { get; set; }

    /// <summary>Gets the free-text notes accumulated during this table's migration (backfills applied, duplicates found, etc.).</summary>
    public List<string> Notes { get; init; } = new();

    /// <summary>Gets or sets the wall-clock duration this table's migration took.</summary>
    public TimeSpan Duration { get; set; }
}
