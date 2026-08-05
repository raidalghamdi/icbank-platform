using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>intl_day_sources</c> → <see cref="IntlDaySource"/>.</summary>
/// <remarks>
/// <c>related_id</c> is a polymorphic reference (<c>related_table</c> discriminates the target),
/// but DATA-MODEL.md and the destination entity's own remarks confirm it is only ever used for
/// <c>international_days</c> in production. When <c>related_table == "international_days"</c>
/// this migrator re-points both <see cref="IntlDaySource.RelatedId"/> (kept verbatim as the
/// polymorphic column for fidelity) and populates the convenience <see cref="IntlDaySource.DayId"/>
/// through the id-mapping store. If a future/unexpected row uses a different
/// <c>related_table</c>, <see cref="IntlDaySource.RelatedId"/> is still copied verbatim (it is
/// meaningless without knowing which table it targets, but dropping it would lose the only
/// evidence the row ever pointed anywhere) and <see cref="IntlDaySource.DayId"/> is left null;
/// this is called out as a reconciliation item if it ever occurs, not silently miscategorized.
/// The source table has no timestamp columns other than <c>accessed_at</c>, which is also used to
/// backfill <c>CreatedAt</c> (the same synthetic-timestamp pattern used elsewhere in this tool).
/// </remarks>
public sealed class IntlDaySourceTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "intl_day_sources";

    /// <inheritdoc />
    public string DestinationTableName => "intl_day_sources";

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

            var relatedTable = row.GetString("related_table");
            var relatedId = row.GetInt32("related_id");

            int? dayId = null;
            if (relatedTable == "international_days")
            {
                dayId = await context.IdMap.TryGetDestinationIdAsync("international_days", relatedId, cancellationToken);
                if (!dayId.HasValue)
                {
                    result.Notes.Add($"Source row id={sourceId} targets international_days id={relatedId}, which was not migrated; DayId left null.");
                }
            }
            else
            {
                result.Notes.Add($"Source row id={sourceId} has unexpected related_table='{relatedTable}' (expected 'international_days'); RelatedId copied verbatim, DayId left null.");
            }

            DateTime accessedAtRaw = row.GetRawTimestamp("accessed_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new IntlDaySource
            {
                RelatedTable = relatedTable,
                RelatedId = relatedId,
                DayId = dayId,
                SourceUrl = row.GetNullableString("source_url"),
                SourceTitle = row.GetNullableString("source_title"),
                SourcePublisher = row.GetNullableString("source_publisher"),
                AccessedAt = new DateTimeOffset(accessedAtRaw, TimeSpan.Zero),
                CreatedAt = accessedAtRaw,
                CreatedBy = "data-migration-tool",
            };

            destination.IntlDaySources.Add(entity);
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
        return await destination.IntlDaySources.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
