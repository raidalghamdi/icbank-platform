using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>user_roles</c> → <see cref="UserRole"/>. Depends on <see cref="UserTableMigrator"/>
/// and <see cref="RoleTableMigrator"/>. See <see cref="UserRoleTransformer"/> for the multi-role
/// migration decision (every row is carried over, not just the first).
/// </summary>
public sealed class UserRoleTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "user_roles";

    /// <inheritdoc />
    public string DestinationTableName => "user_roles";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        await using AppDbContext destination = context.CreateDestinationContext();
        int usersWithMoreThanOneRole = 0;
        var roleCountPerUser = new Dictionary<int, int>();

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            MappedUserRole mapped = UserRoleTransformer.Transform(row);

            roleCountPerUser[mapped.UserSourceId] = roleCountPerUser.GetValueOrDefault(mapped.UserSourceId) + 1;
            if (roleCountPerUser[mapped.UserSourceId] == 2)
            {
                usersWithMoreThanOneRole++;
            }

            int? existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, mapped.SourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            int? userId = await context.IdMap.TryGetDestinationIdAsync("users", mapped.UserSourceId, cancellationToken);
            int? roleId = await context.IdMap.TryGetDestinationIdAsync("roles", mapped.RoleSourceId, cancellationToken);
            int? assignedById = mapped.AssignedBySourceId.HasValue
                ? await context.IdMap.TryGetDestinationIdAsync("users", mapped.AssignedBySourceId.Value, cancellationToken)
                : null;

            if (userId is null || roleId is null)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"user_roles source id {mapped.SourceId}: orphaned FK (user/role not yet migrated) — skipped.");
                continue;
            }

            var entity = new UserRole
            {
                UserId = userId.Value,
                RoleId = roleId.Value,
                AssignedById = assignedById,
                AssignedAt = mapped.AssignedAtUtc,
                CreatedAt = mapped.AssignedAtUtc,
                CreatedBy = "data-migration-tool",
            };

            destination.UserRoles.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, mapped.SourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            result.RowsInserted++;
        }

        if (usersWithMoreThanOneRole > 0)
        {
            result.Notes.Add(
                $"{usersWithMoreThanOneRole} user(s) have more than one user_roles row. All rows were migrated " +
                "(multi-role union, not Node's first-role-only .limit(1) behavior) — their effective permission " +
                "set after cutover may be broader than what the old Node UI ever surfaced. Flagged for product review.");
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.UserRoles.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
