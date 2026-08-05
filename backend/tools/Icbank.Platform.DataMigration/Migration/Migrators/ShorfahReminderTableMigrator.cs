using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>shorfah_reminders</c> → <see cref="ShorfahReminder"/>. Depends on
/// <see cref="ShorfahSectionTableMigrator"/> (required <c>section_id</c> FK),
/// <see cref="ShorfahAssignmentTableMigrator"/> (optional <c>assignment_id</c> FK), and
/// <see cref="UserTableMigrator"/> (required <c>recipient_user_id</c> FK).
/// </summary>
public sealed class ShorfahReminderTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "shorfah_reminders";

    /// <inheritdoc />
    public string DestinationTableName => "shorfah_reminders";

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
            var sourceRecipientUserId = row.GetInt32("recipient_user_id");
            var sectionId = await context.IdMap.TryGetDestinationIdAsync("shorfah_sections", sourceSectionId, cancellationToken);
            var recipientUserId = await context.IdMap.TryGetDestinationIdAsync("users", sourceRecipientUserId, cancellationToken);

            if (sectionId is null || recipientUserId is null)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"shorfah_reminders source id {sourceId}: orphaned FK (section {sourceSectionId} and/or recipient user {sourceRecipientUserId} not yet migrated) — skipped.");
                continue;
            }

            var sourceAssignmentId = row.GetNullableInt32("assignment_id");
            var assignmentId = sourceAssignmentId.HasValue
                ? await context.IdMap.TryGetDestinationIdAsync("shorfah_assignments", sourceAssignmentId.Value, cancellationToken)
                : null;
            if (sourceAssignmentId.HasValue && assignmentId is null)
            {
                result.Notes.Add($"shorfah_reminders source id {sourceId}: assignment_id {sourceAssignmentId} not found in id-map — left null.");
            }

            var entity = new ShorfahReminder
            {
                SectionId = sectionId.Value,
                AssignmentId = assignmentId,
                RecipientUserId = recipientUserId.Value,
                Channel = SnakeCaseEnumParser.Parse<ShorfahReminderChannel>(row.GetString("channel")),
                ReminderType = SnakeCaseEnumParser.Parse<ShorfahReminderType>(row.GetString("reminder_type")),
                SentAt = TimestampConverter.ToDestinationOffset(row.GetRawTimestamp("sent_at")),
                Status = row.GetNullableString("status"),
                Message = row.GetNullableString("message"),
                CreatedAt = context.DateTimeProvider.UtcNow.UtcDateTime,
                CreatedBy = "data-migration-tool",
            };

            destination.ShorfahReminders.Add(entity);
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
        return await destination.ShorfahReminders.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
