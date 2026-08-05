using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Weekend;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>weekend_places</c> → <see cref="WeekendPlace"/>.</summary>
public sealed class WeekendPlaceTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "weekend_places";

    /// <inheritdoc />
    public string DestinationTableName => "weekend_places";

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

            var entity = new WeekendPlace
            {
                Name = row.GetString("name"),
                Description = row.GetString("description"),
                ImageUrl = row.GetNullableString("image_url"),
                City = string.IsNullOrEmpty(row.GetNullableString("city")) ? "الرياض" : row.GetString("city"),
                MapsQuery = row.GetNullableString("maps_query"),
                IsActive = row.GetBoolean("is_active"),
                SortOrder = row.GetNullableInt32("sort_order") ?? 0,
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.WeekendPlaces.Add(entity);
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
        return await destination.WeekendPlaces.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
