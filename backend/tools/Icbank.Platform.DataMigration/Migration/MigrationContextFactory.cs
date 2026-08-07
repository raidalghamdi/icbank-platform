using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration;

/// <summary>
/// Builds <see cref="AppDbContext"/> instances for the migration tool's own use.
/// </summary>
/// <remarks>
/// <para><b>Design decision — no <c>AuditInterceptor</c> (see docs/DATA-MIGRATION.md and
/// spec/DATA-MIGRATION-NOTES.md):</b> the application's runtime <c>AuditInterceptor</c>
/// unconditionally overwrites <c>CreatedAt</c>/<c>CreatedBy</c> on insert and
/// <c>UpdatedAt</c>/<c>UpdatedBy</c> on update with "now" and the current user. A migration must
/// preserve the source system's real historical timestamps, so this factory builds
/// <see cref="AppDbContext"/> without registering that interceptor at all — every table
/// transformer sets <c>CreatedAt</c>/<c>CreatedBy</c> explicitly from the mapped source data
/// instead. This is scoped entirely to this tool's own composition root
/// (<c>Icbank.Platform.DataMigration</c>); the running API's <c>DependencyInjection.AddPersistence</c>
/// is untouched, so there is zero behavior change or regression risk to the rest of the
/// platform.</para>
/// </remarks>
public static class MigrationContextFactory
{
    /// <summary>Creates a new <see cref="AppDbContext"/> against the destination SQL Server database, without the runtime audit interceptor.</summary>
    /// <param name="connectionString">The destination SQL Server connection string.</param>
    /// <returns>A ready-to-use context. Caller owns disposal.</returns>
    public static AppDbContext Create(string connectionString)
    {
        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new AppDbContext(options);
    }
}
