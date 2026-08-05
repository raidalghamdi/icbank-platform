using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>permissions</c> → <see cref="Permission"/>, keyed on the natural key <c>name</c> (<c>ux_permissions_name</c>).</summary>
public sealed class PermissionTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "permissions";

    /// <inheritdoc />
    public string DestinationTableName => "permissions";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        await using AppDbContext destination = context.CreateDestinationContext();

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            int sourceId = row.GetInt32("id");

            int? existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, sourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            string name = row.GetString("name");
            Permission? existingByName = await destination.Permissions.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Name == name, cancellationToken);
            if (existingByName is not null)
            {
                result.RowsSkippedAlreadyMigrated++;
                await context.IdMap.RecordAsync(SourceTableName, sourceId, existingByName.Id, context.DateTimeProvider.UtcNow, cancellationToken);
                continue;
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new Permission
            {
                Name = name,
                NameAr = row.GetString("name_ar"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.Permissions.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, sourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            result.RowsInserted++;
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.Permissions.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
