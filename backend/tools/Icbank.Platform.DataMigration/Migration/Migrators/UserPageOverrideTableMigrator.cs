using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>user_page_overrides</c> → <see cref="UserPageOverride"/>.</summary>
/// <remarks>
/// <c>user_id</c>, <c>page_id</c> and <c>permission_id</c> are required, enforced FKs in both
/// source and destination; a row whose referenced user/page/permission was not migrated (should
/// not happen given the registry's FK-safe ordering, but is defensively checked) is skipped
/// entirely rather than inserted with a dangling/zero id, and counted as
/// <see cref="TableMigrationResult.RowsSkippedDueToDataIssue"/>.
/// </remarks>
public sealed class UserPageOverrideTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "user_page_overrides";

    /// <inheritdoc />
    public string DestinationTableName => "user_page_overrides";

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

            var userId = await context.IdMap.TryGetDestinationIdAsync("users", row.GetInt32("user_id"), cancellationToken);
            var pageId = await context.IdMap.TryGetDestinationIdAsync("pages", row.GetInt32("page_id"), cancellationToken);
            var permissionId = await context.IdMap.TryGetDestinationIdAsync("permissions", row.GetInt32("permission_id"), cancellationToken);

            if (!userId.HasValue || !pageId.HasValue || !permissionId.HasValue)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"Source row id={sourceId} references a user/page/permission id that was not migrated; skipped.");
                continue;
            }

            int? createdByUserId = null;
            var sourceCreatedBy = row.GetNullableInt32("created_by");
            if (sourceCreatedBy.HasValue)
            {
                createdByUserId = await context.IdMap.TryGetDestinationIdAsync("users", sourceCreatedBy.Value, cancellationToken);
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new UserPageOverride
            {
                UserId = userId.Value,
                PageId = pageId.Value,
                PermissionId = permissionId.Value,
                GrantType = SnakeCaseEnumParser.Parse<OverrideGrantType>(row.GetString("grant_type")),
                CreatedByUserId = createdByUserId,
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.UserPageOverrides.Add(entity);
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
        return await destination.UserPageOverrides.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
