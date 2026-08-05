using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>role_permissions</c> → <see cref="RolePermission"/>. Depends on
/// <see cref="RoleTableMigrator"/>, <see cref="PageTableMigrator"/>, and
/// <see cref="PermissionTableMigrator"/> having already run — resolves each source FK through
/// the id-mapping store rather than assuming ids line up (they never will, since destination ids
/// are freshly assigned by SQL Server's identity column).
/// </summary>
public sealed class RolePermissionTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "role_permissions";

    /// <inheritdoc />
    public string DestinationTableName => "role_permissions";

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

            var sourceRoleId = row.GetInt32("role_id");
            var sourcePageId = row.GetInt32("page_id");
            var sourcePermissionId = row.GetInt32("permission_id");

            var roleId = await context.IdMap.TryGetDestinationIdAsync("roles", sourceRoleId, cancellationToken);
            var pageId = await context.IdMap.TryGetDestinationIdAsync("pages", sourcePageId, cancellationToken);
            var permissionId = await context.IdMap.TryGetDestinationIdAsync("permissions", sourcePermissionId, cancellationToken);

            if (roleId is null || pageId is null || permissionId is null)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"role_permissions source id {sourceId}: orphaned FK (role/page/permission not yet migrated) — skipped.");
                continue;
            }

            var alreadyExists = await destination.RolePermissions.IgnoreQueryFilters()
                .AnyAsync(rp => rp.RoleId == roleId && rp.PageId == pageId && rp.PermissionId == permissionId, cancellationToken);
            if (alreadyExists)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new RolePermission
            {
                RoleId = roleId.Value,
                PageId = pageId.Value,
                PermissionId = permissionId.Value,
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.RolePermissions.Add(entity);
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
        return await destination.RolePermissions.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
