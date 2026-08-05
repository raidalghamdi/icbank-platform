using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>shorfah_issues</c> → <see cref="ShorfahIssue"/>, keyed on the natural key
/// <c>issue_no</c> (source <c>unique()</c> constraint). Must run before
/// <see cref="ShorfahSectionTableMigrator"/> (sections FK to their owning issue) and after
/// <see cref="UserTableMigrator"/> (optional <c>created_by</c> FK).
/// </summary>
/// <remarks>
/// See <see cref="ShorfahIssueTransformer"/> for the AMBIGUOUS-8 nullable-audit-timestamp
/// backfill decision (same pattern as <see cref="ShorfahSectionTableMigrator"/>); every
/// backfilled value is recorded as a report note here, never silently applied.
/// </remarks>
public sealed class ShorfahIssueTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "shorfah_issues";

    /// <inheritdoc />
    public string DestinationTableName => "shorfah_issues";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;
        DateTime migrationRunTimestamp = context.DateTimeProvider.UtcNow.UtcDateTime;
        var backfillCount = 0;

        await using AppDbContext destination = context.CreateDestinationContext();

        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            MappedShorfahIssue mapped = ShorfahIssueTransformer.Transform(row, migrationRunTimestamp);

            var existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, mapped.SourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            ShorfahIssue? existingByIssueNo = await destination.ShorfahIssues.IgnoreQueryFilters()
                .FirstOrDefaultAsync(i => i.IssueNo == mapped.IssueNo, cancellationToken);
            if (existingByIssueNo is not null)
            {
                result.RowsSkippedAlreadyMigrated++;
                await context.IdMap.RecordAsync(SourceTableName, mapped.SourceId, existingByIssueNo.Id, context.DateTimeProvider.UtcNow, cancellationToken);
                continue;
            }

            var createdByUserId = mapped.CreatedBySourceId.HasValue
                ? await context.IdMap.TryGetDestinationIdAsync("users", mapped.CreatedBySourceId.Value, cancellationToken)
                : null;

            if (mapped.CreatedBySourceId.HasValue && createdByUserId is null)
            {
                result.Notes.Add($"shorfah_issues source id {mapped.SourceId}: created_by {mapped.CreatedBySourceId} not found in users id-map — left null (unenforced FK in source, DATA-MODEL.md §4).");
            }

            var entity = new ShorfahIssue
            {
                IssueNo = mapped.IssueNo,
                TitleAr = mapped.TitleAr,
                SubtitleAr = mapped.SubtitleAr,
                Month = mapped.Month,
                Year = mapped.Year,
                CoverImageUrl = mapped.CoverImageUrl,
                EditorLetter = mapped.EditorLetter,
                ContributionsOpenAt = TimestampConverter.ToDestinationOffset(mapped.ContributionsOpenAtUtc),
                ContributionsCloseAt = TimestampConverter.ToDestinationOffset(mapped.ContributionsCloseAtUtc),
                Status = SnakeCaseEnumParser.Parse<ShorfahIssueStatus>(mapped.Status),
                PublishedPdfUrl = mapped.PublishedPdfUrl,
                PublishedAt = TimestampConverter.ToDestinationOffset(mapped.PublishedAtUtc),
                CreatedByUserId = createdByUserId,
                CreatedAt = mapped.CreatedAtUtc,
                CreatedBy = "data-migration-tool",
                UpdatedAt = mapped.UpdatedAtUtc,
            };

            destination.ShorfahIssues.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, mapped.SourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            result.RowsInserted++;

            if (mapped.CreatedAtBackfilled || mapped.UpdatedAtBackfilled)
            {
                backfillCount++;
            }
        }

        if (backfillCount > 0)
        {
            result.Notes.Add($"{backfillCount} shorfah_issues row(s) had a nullable-in-source audit timestamp backfilled (AMBIGUOUS-8) — see docs/DATA-MIGRATION.md.");
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.ShorfahIssues.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
