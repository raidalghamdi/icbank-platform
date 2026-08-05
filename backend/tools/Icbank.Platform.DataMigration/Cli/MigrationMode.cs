namespace Icbank.Platform.DataMigration.Cli;

/// <summary>The three operating modes this tool supports (task requirements 4-6).</summary>
public enum MigrationMode
{
    /// <summary>Pre-flight, read-only checks against source and destination — no writes.</summary>
    Validate,

    /// <summary>Runs the actual table-by-table migration.</summary>
    Migrate,

    /// <summary>Post-migration source vs destination comparison.</summary>
    Reconcile,
}
