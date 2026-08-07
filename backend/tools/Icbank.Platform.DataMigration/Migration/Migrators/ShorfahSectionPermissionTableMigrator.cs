using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>shorfah_section_permissions</c> → <see cref="ShorfahSectionPermission"/>. Depends
/// on <see cref="ShorfahSectionTableMigrator"/> (required <c>section_id</c> FK) and
/// <see cref="UserTableMigrator"/> (optional <c>user_id</c> FK — a grant may instead target
/// <c>role_name</c>, mutually exclusive with <c>user_id</c> by convention, not enforced).
/// </summary>
public sealed class ShorfahSectionPermissionTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "shorfah_section_permissions";

    /// <inheritdoc />
    public string DestinationTableName => "shorfah_section_permissions";

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

            var sourceSectionId = row.GetInt32("section_id");
            var sectionId = await context.IdMap.TryGetDestinationIdAsync("shorfah_sections", sourceSectionId, cancellationToken);
            if (sectionId is null)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"shorfah_section_permissions source id {sourceId}: orphaned section_id {sourceSectionId} — skipped.");
                continue;
            }

            var sourceUserId = row.GetNullableInt32("user_id");
            var userId = sourceUserId.HasValue
                ? await context.IdMap.TryGetDestinationIdAsync("users", sourceUserId.Value, cancellationToken)
                : null;
            if (sourceUserId.HasValue && userId is null)
            {
                result.Notes.Add($"shorfah_section_permissions source id {sourceId}: user_id {sourceUserId} not found in users id-map — left null.");
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new ShorfahSectionPermission
            {
                SectionId = sectionId.Value,
                UserId = userId,
                RoleName = row.GetNullableString("role_name"),
                Permission = SnakeCaseEnumParser.Parse<ShorfahPermissionVerb>(row.GetString("permission")),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.ShorfahSectionPermissions.Add(entity);
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
        return await destination.ShorfahSectionPermissions.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
