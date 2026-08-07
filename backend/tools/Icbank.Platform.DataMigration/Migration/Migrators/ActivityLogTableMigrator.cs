using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>activity_logs</c> → <see cref="ActivityLog"/> (audit history).</summary>
/// <remarks>
/// <c>user_id</c> is an optional FK with <c>onDelete: "set null"</c> in the source, so an
/// unmapped source user id is set to <see langword="null"/> here rather than skipping the row --
/// the audit event itself is still historically meaningful even if the actor cannot be resolved.
/// <c>details</c> (untyped jsonb) is carried through verbatim as JSON text into
/// <see cref="ActivityLog.DetailsJson"/> rather than deserialized into any typed shape, matching
/// the destination entity's own untyped-JSON-text modeling of this column.
/// </remarks>
public sealed class ActivityLogTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "activity_logs";

    /// <inheritdoc />
    public string DestinationTableName => "activity_logs";

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

            int? userId = null;
            var sourceUserId = row.GetNullableInt32("user_id");
            if (sourceUserId.HasValue)
            {
                userId = await context.IdMap.TryGetDestinationIdAsync("users", sourceUserId.Value, cancellationToken);
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;
            var detailsRaw = row["details"];

            var entity = new ActivityLog
            {
                UserId = userId,
                Action = row.GetString("action"),
                EntityType = row.GetNullableString("entity_type"),
                EntityId = row.GetNullableString("entity_id"),
                DetailsJson = detailsRaw is null ? null : row.ReadRawJsonText("details", "null"),
                IpAddress = row.GetNullableString("ip_address"),
                UserAgent = row.GetNullableString("user_agent"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.ActivityLogs.Add(entity);
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
        return await destination.ActivityLogs.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
