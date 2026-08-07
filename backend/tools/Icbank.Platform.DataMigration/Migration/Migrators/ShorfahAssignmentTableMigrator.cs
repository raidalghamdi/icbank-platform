using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>shorfah_assignments</c> → <see cref="ShorfahAssignment"/>. Depends on
/// <see cref="ShorfahSectionTableMigrator"/> (required <c>section_id</c> FK) and
/// <see cref="UserTableMigrator"/> (required <c>user_id</c> FK). Must run before
/// <see cref="ShorfahReminderTableMigrator"/>, which optionally FKs to an assignment.
/// </summary>
public sealed class ShorfahAssignmentTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "shorfah_assignments";

    /// <inheritdoc />
    public string DestinationTableName => "shorfah_assignments";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        await using AppDbContext destination = context.CreateDestinationContext();

        var sourceRows = new List<SourceRow>();
        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            sourceRows.Add(row);
        }

        (IReadOnlyList<SourceRow> rowsToMigrate, IReadOnlyList<SourceRow> supersededRows) =
            ShorfahAssignmentDeduplicator.SelectLastWrites(sourceRows);
        var supersededSourceIdsByWinner = sourceRows
            .GroupBy(row => (SectionId: row.GetInt32("section_id"), UserId: row.GetInt32("user_id")))
            .ToDictionary(
                group => group.Max(row => row.GetInt32("id")),
                group => group
                    .Select(row => row.GetInt32("id"))
                    .Where(sourceId => sourceId != group.Max(row => row.GetInt32("id")))
                    .ToArray());
        foreach (SourceRow supersededRow in supersededRows)
        {
            result.RowsSkippedDueToDataIssue++;
            result.Notes.Add(
                $"shorfah_assignments source id {supersededRow.GetInt32("id")}: rejected as a superseded duplicate " +
                $"for section/user ({supersededRow.GetInt32("section_id")}, {supersededRow.GetInt32("user_id")}); " +
                "highest source id is retained by the explicit last-write-wins rule.");
        }

        foreach (SourceRow row in rowsToMigrate)
        {
            var sourceId = row.GetInt32("id");
            var existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, sourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            var sourceSectionId = row.GetInt32("section_id");
            var sourceUserId = row.GetInt32("user_id");
            var sectionId = await context.IdMap.TryGetDestinationIdAsync("shorfah_sections", sourceSectionId, cancellationToken);
            var userId = await context.IdMap.TryGetDestinationIdAsync("users", sourceUserId, cancellationToken);

            if (sectionId is null || userId is null)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"shorfah_assignments source id {sourceId}: orphaned FK (section {sourceSectionId} and/or user {sourceUserId} not yet migrated) — skipped.");
                continue;
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new ShorfahAssignment
            {
                SectionId = sectionId.Value,
                UserId = userId.Value,
                Role = row.GetNullableString("role"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.ShorfahAssignments.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, sourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            foreach (var supersededSourceId in supersededSourceIdsByWinner[sourceId])
            {
                await context.IdMap.RecordAsync(
                    SourceTableName,
                    supersededSourceId,
                    entity.Id,
                    context.DateTimeProvider.UtcNow,
                    cancellationToken);
            }

            result.RowsInserted++;
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.ShorfahAssignments.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
