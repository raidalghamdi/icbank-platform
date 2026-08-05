namespace Icbank.Platform.DataMigration.Configuration;

/// <summary>
/// Connection and behavior settings for the migration tool, bound from configuration
/// (<c>appsettings.json</c>, environment variables, or .NET user-secrets) — never from
/// command-line arguments, so a connection string never lands in shell history (task
/// requirement 6). See docs/DATA-MIGRATION.md "Configuration" section.
/// </summary>
public sealed class MigrationOptions
{
    /// <summary>The configuration section name this type binds to.</summary>
    public const string SectionName = "Migration";

    /// <summary>Gets or sets the Npgsql connection string for the source Supabase/Postgres database.</summary>
    public string SourceConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the SqlClient connection string for the destination SQL Server database.</summary>
    public string DestinationConnectionString { get; set; } = string.Empty;

    /// <summary>Gets or sets the directory the final report and structured log files are written to.</summary>
    public string ReportDirectory { get; set; } = "migration-reports";

    /// <summary>Gets or sets the number of rows read from Postgres per batch, for memory-bounded streaming.</summary>
    public int BatchSize { get; set; } = 500;
}
