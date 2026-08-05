using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>shorfah_notifications</c> → <see cref="ShorfahNotification"/> (task priority:
/// "notifications"). Depends on <see cref="UserTableMigrator"/> (required <c>user_id</c> FK),
/// <see cref="ShorfahIssueTableMigrator"/> (optional <c>issue_id</c> FK), and
/// <see cref="ShorfahSectionTableMigrator"/> (optional <c>section_id</c> FK). <c>type</c> is
/// kept as free text (not parsed into an enum) — the source comment documents open-ended values
/// like <c>"initial"</c>, <c>"reminder_overdue"</c>, <c>"published"</c> with no closed set
/// declared anywhere in DATA-MODEL.md, so parsing it would risk silently dropping notification
/// rows whose type string does not match a guessed enum member.
/// </summary>
public sealed class ShorfahNotificationTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "shorfah_notifications";

    /// <inheritdoc />
    public string DestinationTableName => "shorfah_notifications";

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

            var sourceUserId = row.GetInt32("user_id");
            var userId = await context.IdMap.TryGetDestinationIdAsync("users", sourceUserId, cancellationToken);
            if (userId is null)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"shorfah_notifications source id {sourceId}: orphaned user_id {sourceUserId} — skipped.");
                continue;
            }

            var sourceIssueId = row.GetNullableInt32("issue_id");
            var issueId = sourceIssueId.HasValue
                ? await context.IdMap.TryGetDestinationIdAsync("shorfah_issues", sourceIssueId.Value, cancellationToken)
                : null;
            if (sourceIssueId.HasValue && issueId is null)
            {
                result.Notes.Add($"shorfah_notifications source id {sourceId}: issue_id {sourceIssueId} not found in id-map — left null.");
            }

            var sourceSectionId = row.GetNullableInt32("section_id");
            var sectionId = sourceSectionId.HasValue
                ? await context.IdMap.TryGetDestinationIdAsync("shorfah_sections", sourceSectionId.Value, cancellationToken)
                : null;
            if (sourceSectionId.HasValue && sectionId is null)
            {
                result.Notes.Add($"shorfah_notifications source id {sourceId}: section_id {sourceSectionId} not found in id-map — left null.");
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new ShorfahNotification
            {
                UserId = userId.Value,
                IssueId = issueId,
                SectionId = sectionId,
                Type = row.GetString("type"),
                Title = row.GetString("title"),
                Body = row.GetNullableString("body"),
                Url = row.GetNullableString("url"),
                IsRead = row.GetNullableBoolean("is_read"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.ShorfahNotifications.Add(entity);
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
        return await destination.ShorfahNotifications.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
