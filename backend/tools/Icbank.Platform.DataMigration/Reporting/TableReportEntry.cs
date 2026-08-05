namespace Icbank.Platform.DataMigration.Reporting;

/// <summary>One table's contribution to the report, shared shape across all three modes.</summary>
public sealed class TableReportEntry
{
    /// <summary>Gets the source table name.</summary>
    public required string TableName { get; init; }

    /// <summary>Gets or sets the source row count.</summary>
    public long SourceRowCount { get; set; }

    /// <summary>Gets or sets the destination row count (post-migration / reconciliation modes).</summary>
    public long? DestinationRowCount { get; set; }

    /// <summary>Gets the free-text notes/issues for this table.</summary>
    public List<string> Notes { get; init; } = new();

    /// <summary>Gets or sets a value indicating whether this table passed its checks.</summary>
    public bool Pass { get; set; } = true;
}
