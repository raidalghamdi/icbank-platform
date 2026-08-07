using System.Collections.Concurrent;

namespace Icbank.Platform.DataMigration.Reporting;

/// <summary>
/// Accumulates the findings of one tool run (validation, migration, or reconciliation) so they
/// can be written to a final report file (task requirement 6). Thread-safe: table migrators may
/// run sequentially today (FK-safe order requires it) but the type is defensive against future
/// parallelization of independent subtrees.
/// </summary>
public sealed class MigrationReport
{
    private readonly ConcurrentBag<string> _findings = new();

    /// <summary>Gets the mode this report was generated under.</summary>
    public required string Mode { get; init; }

    /// <summary>Gets the UTC instant the run started.</summary>
    public required DateTimeOffset StartedAtUtc { get; init; }

    /// <summary>Gets or sets the UTC instant the run finished.</summary>
    public DateTimeOffset FinishedAtUtc { get; set; }

    /// <summary>Gets the per-table results, in the order tables were processed.</summary>
    public List<TableReportEntry> Tables { get; } = new();

    /// <summary>Gets free-text, run-level findings not tied to a specific table (e.g. password-portability summary).</summary>
    public IReadOnlyCollection<string> Findings => _findings;

    /// <summary>Gets or sets a value indicating whether the run is considered an overall pass (used by validation/reconciliation modes).</summary>
    public bool OverallPass { get; set; } = true;

    /// <summary>Adds a run-level finding.</summary>
    /// <param name="finding">The finding text.</param>
    public void AddFinding(string finding) => _findings.Add(finding);
}
