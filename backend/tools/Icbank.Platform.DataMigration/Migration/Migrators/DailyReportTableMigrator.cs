using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Validation;
using Icbank.Platform.Domain.Reports;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>daily_reports</c> → <see cref="DailyReport"/>. Detects rows that would violate
/// the new <c>ux_daily_reports_report_date</c> unique index (DATA-MODEL.md section 3.3 flags
/// <c>report_date</c> as only an "implied UNIQUE" in the source, enforced by app-level
/// select-then-upsert rather than a real database constraint, so a race between two concurrent
/// POSTs could have produced duplicate source rows for the same date) and reports them instead
/// of letting the insert throw: for each duplicate group, the earliest-created row (by source
/// <c>created_at</c>) is migrated and every other row in the group is skipped and named in the
/// report, matching the same pattern already used by <see cref="GacSocialPostTableMigrator"/>
/// for its own new unique index.
/// </summary>
public sealed class DailyReportTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "daily_reports";

    /// <inheritdoc />
    public string DestinationTableName => "daily_reports";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        var mappedRows = new List<MappedDailyReport>();
        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            mappedRows.Add(DailyReportTransformer.Transform(row));
        }

        IReadOnlyList<DuplicateKeyGroup<DateOnly>> duplicates =
            DuplicateKeyDetector.FindDuplicates(mappedRows, m => m.ReportDate, m => m.SourceId);

        var sourceIdsToSkip = new HashSet<int>();
        foreach (DuplicateKeyGroup<DateOnly> group in duplicates)
        {
            var keepSourceId = group.SourceIds
                .Select(id => mappedRows.First(m => m.SourceId == id))
                .OrderBy(m => m.CreatedAtUtc)
                .First()
                .SourceId;

            IEnumerable<int> skipped = group.SourceIds.Where(id => id != keepSourceId);
            foreach (var id in skipped)
            {
                sourceIdsToSkip.Add(id);
            }

            result.Notes.Add(
                $"Duplicate report_date ({group.Key:yyyy-MM-dd}): source ids [{string.Join(", ", group.SourceIds)}] " +
                $"— kept earliest-created id {keepSourceId}, skipped [{string.Join(", ", skipped)}] to satisfy the new unique index.");
        }

        await using AppDbContext destination = context.CreateDestinationContext();

        foreach (MappedDailyReport mapped in mappedRows)
        {
            if (sourceIdsToSkip.Contains(mapped.SourceId))
            {
                result.RowsSkippedDueToDataIssue++;
                continue;
            }

            var existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, mapped.SourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            var alreadyExists = await destination.DailyReports.IgnoreQueryFilters()
                .AnyAsync(r => r.ReportDate == mapped.ReportDate, cancellationToken);
            if (alreadyExists)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            var entity = new DailyReport
            {
                ReportDate = mapped.ReportDate,
                ReportDataJson = mapped.ReportDataJson,
                CreatedAt = mapped.CreatedAtUtc,
                CreatedBy = "data-migration-tool",
            };

            destination.DailyReports.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, mapped.SourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            result.RowsInserted++;
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.DailyReports.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
