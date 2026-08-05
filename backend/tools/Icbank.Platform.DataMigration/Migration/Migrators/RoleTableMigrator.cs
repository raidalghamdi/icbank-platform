using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>roles</c> → <see cref="Role"/>, keyed on the natural key <c>name</c> (<c>ux_roles_name</c>).</summary>
public sealed class RoleTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "roles";

    /// <inheritdoc />
    public string DestinationTableName => "roles";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        await using AppDbContext destination = context.CreateDestinationContext();

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            var sourceId = row.GetInt32("id");

            var existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, sourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            var name = row.GetString("name");
            Role? existingByName = await destination.Roles.IgnoreQueryFilters().FirstOrDefaultAsync(r => r.Name == name, cancellationToken);
            if (existingByName is not null)
            {
                result.RowsSkippedAlreadyMigrated++;
                await context.IdMap.RecordAsync(SourceTableName, sourceId, existingByName.Id, context.DateTimeProvider.UtcNow, cancellationToken);
                continue;
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new Role
            {
                Name = name,
                NameAr = row.GetString("name_ar"),
                Description = row.GetNullableString("description"),
                IsSystem = row.GetBoolean("is_system"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.Roles.Add(entity);
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
        return await destination.Roles.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
