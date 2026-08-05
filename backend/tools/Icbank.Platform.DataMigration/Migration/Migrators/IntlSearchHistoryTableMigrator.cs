using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>intl_search_history</c> → <see cref="IntlSearchHistory"/>.</summary>
/// <remarks>
/// <c>day_id</c> was an unenforced implied FK in the source; an unmapped source id is set to
/// <see langword="null"/> here rather than dropping the row, since the search-history event
/// itself is still meaningful even if its day association cannot be resolved. The source table
/// has no <c>created_at</c>; <c>searched_at</c> backfills both <c>SearchedAt</c> and the base
/// <c>CreatedAt</c> audit column.
/// </remarks>
public sealed class IntlSearchHistoryTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "intl_search_history";

    /// <inheritdoc />
    public string DestinationTableName => "intl_search_history";

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

            int? dayId = null;
            var sourceDayId = row.GetNullableInt32("day_id");
            if (sourceDayId.HasValue)
            {
                dayId = await context.IdMap.TryGetDestinationIdAsync("international_days", sourceDayId.Value, cancellationToken);
            }

            DateTime searchedAtRaw = row.GetRawTimestamp("searched_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new IntlSearchHistory
            {
                Query = row.GetString("query"),
                DayId = dayId,
                IpAddress = row.GetNullableString("ip_address"),
                SearchedAt = new DateTimeOffset(searchedAtRaw, TimeSpan.Zero),
                CreatedAt = searchedAtRaw,
                CreatedBy = "data-migration-tool",
            };

            destination.IntlSearchHistories.Add(entity);
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
        return await destination.IntlSearchHistories.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
