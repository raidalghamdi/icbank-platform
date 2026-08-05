using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>shorfah_workflow_log</c> → <see cref="ShorfahWorkflowLog"/> — the full audit
/// trail of every workflow transition per section (task priority: "audit history"). Depends on
/// <see cref="ShorfahSectionTableMigrator"/> (required <c>section_id</c> FK) and
/// <see cref="UserTableMigrator"/> (optional <c>actor_user_id</c> FK). <c>from_status</c>/
/// <c>to_status</c> are kept as free text (not parsed into <see cref="ShorfahWorkflowStatus"/>)
/// because they are a historical record of whatever string value was current at the time of the
/// transition — parsing them into today's enum could silently misrepresent an old status name
/// that no longer exists.
/// </summary>
public sealed class ShorfahWorkflowLogTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "shorfah_workflow_log";

    /// <inheritdoc />
    public string DestinationTableName => "shorfah_workflow_log";

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
                result.Notes.Add($"shorfah_workflow_log source id {sourceId}: orphaned section_id {sourceSectionId} — skipped.");
                continue;
            }

            var sourceActorUserId = row.GetNullableInt32("actor_user_id");
            var actorUserId = sourceActorUserId.HasValue
                ? await context.IdMap.TryGetDestinationIdAsync("users", sourceActorUserId.Value, cancellationToken)
                : null;
            if (sourceActorUserId.HasValue && actorUserId is null)
            {
                result.Notes.Add($"shorfah_workflow_log source id {sourceId}: actor_user_id {sourceActorUserId} not found in users id-map — left null.");
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new ShorfahWorkflowLog
            {
                SectionId = sectionId.Value,
                ActorUserId = actorUserId,
                Action = row.GetString("action"),
                FromStatus = row.GetNullableString("from_status"),
                ToStatus = row.GetNullableString("to_status"),
                Notes = row.GetNullableString("notes"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.ShorfahWorkflowLogs.Add(entity);
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
        return await destination.ShorfahWorkflowLogs.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
