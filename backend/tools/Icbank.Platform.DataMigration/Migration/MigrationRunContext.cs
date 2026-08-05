using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.DataMigration.Reporting;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.Logging;

namespace Icbank.Platform.DataMigration.Migration;

/// <summary>
/// Everything one <see cref="ITableMigrator"/> needs to do its work: the read-only Postgres
/// source, the id-mapping store, a factory for fresh <see cref="AppDbContext"/> instances (per
/// task's architecture requirement: writes always go through EF Core, never hand-written SQL),
/// the clock (per SUBAGENT-RULES.md: Asia/Riyadh time only via <see cref="IDateTimeProvider"/>,
/// never <c>DateTime.Now</c>), a logger, and the shared migration report being built up.
/// </summary>
public sealed class MigrationRunContext
{
    /// <summary>Initializes a new instance of the <see cref="MigrationRunContext"/> class.</summary>
    /// <param name="source">The read-only Postgres data source.</param>
    /// <param name="idMap">The id-mapping store.</param>
    /// <param name="destinationConnectionString">The destination SQL Server connection string, used to open fresh <see cref="AppDbContext"/> instances per table.</param>
    /// <param name="dateTimeProvider">The Asia/Riyadh-aware clock.</param>
    /// <param name="logger">The structured logger.</param>
    /// <param name="report">The migration report being accumulated across all tables.</param>
    public MigrationRunContext(
        IPostgresDataSource source,
        IIdMappingStore idMap,
        string destinationConnectionString,
        IDateTimeProvider dateTimeProvider,
        ILogger logger,
        MigrationReport report)
    {
        Source = source;
        IdMap = idMap;
        DestinationConnectionString = destinationConnectionString;
        DateTimeProvider = dateTimeProvider;
        Logger = logger;
        Report = report;
    }

    /// <summary>Gets the read-only Postgres data source.</summary>
    public IPostgresDataSource Source { get; }

    /// <summary>Gets the id-mapping store.</summary>
    public IIdMappingStore IdMap { get; }

    /// <summary>Gets the destination SQL Server connection string.</summary>
    public string DestinationConnectionString { get; }

    /// <summary>Gets the Asia/Riyadh-aware clock.</summary>
    public IDateTimeProvider DateTimeProvider { get; }

    /// <summary>Gets the structured logger.</summary>
    public ILogger Logger { get; }

    /// <summary>Gets the migration report being accumulated across all tables.</summary>
    public MigrationReport Report { get; }

    /// <summary>Opens a fresh <see cref="AppDbContext"/> against the destination, without the runtime audit interceptor — see <see cref="MigrationContextFactory"/>.</summary>
    /// <returns>A new context. Caller owns disposal.</returns>
    public AppDbContext CreateDestinationContext() => MigrationContextFactory.Create(DestinationConnectionString);
}
