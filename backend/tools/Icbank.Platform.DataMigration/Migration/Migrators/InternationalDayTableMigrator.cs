using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>international_days</c> → <see cref="InternationalDay"/>.</summary>
public sealed class InternationalDayTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "international_days";

    /// <inheritdoc />
    public string DestinationTableName => "international_days";

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

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;
            DateTime? lastSearchedRaw = row.GetRawTimestamp("last_searched_at");

            var entity = new InternationalDay
            {
                DayNameAr = row.GetString("day_name_ar"),
                DayNameEn = row.GetNullableString("day_name_en"),
                AnnualDate = row.GetNullableString("annual_date"),
                Category = row.GetNullableString("category"),
                OfficialOrganizer = row.GetNullableString("official_organizer"),
                OfficialOrganizerSource = row.GetNullableString("official_organizer_source"),
                HistorySummary = row.GetNullableString("history_summary"),
                HistorySource = row.GetNullableString("history_source"),
                Suggestions = row.GetStringArray("suggestions").ToList(),
                LastSearchedAt = lastSearchedRaw.HasValue ? new DateTimeOffset(lastSearchedRaw.Value, TimeSpan.Zero) : null,
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
                UpdatedAt = row.GetRawTimestamp("updated_at"),
            };

            destination.InternationalDays.Add(entity);
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
        return await destination.InternationalDays.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
