using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.InternationalDays;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>day_activations</c> → <see cref="DayActivation"/>.</summary>
/// <remarks>
/// The source table has no timestamp columns at all; <c>CreatedAt</c> is backfilled to the
/// migration run's timestamp, the same synthetic-timestamp treatment already applied to several
/// other tables in this tool (documented individually on each affected migrator).
/// </remarks>
public sealed class DayActivationTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "day_activations";

    /// <inheritdoc />
    public string DestinationTableName => "day_activations";

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

            var dayId = await context.IdMap.TryGetDestinationIdAsync("international_days", row.GetInt32("day_id"), cancellationToken);
            if (!dayId.HasValue)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"Source row id={sourceId} references day_id that was not migrated; skipped.");
                continue;
            }

            var entity = new DayActivation
            {
                DayId = dayId.Value,
                Year = row.GetNullableInt32("year"),
                EntityName = row.GetNullableString("entity_name"),
                EntityType = row.GetNullableString("entity_type"),
                ActivationType = row.GetNullableString("activation_type"),
                Platform = row.GetNullableString("platform"),
                Description = row.GetNullableString("description"),
                MediaUrl = row.GetNullableString("media_url"),
                SourceUrl = row.GetNullableString("source_url"),
                Country = row.GetNullableString("country"),
                Verified = row.GetBoolean("verified"),
                CreatedAt = context.DateTimeProvider.UtcNow.UtcDateTime,
                CreatedBy = "data-migration-tool",
            };

            destination.DayActivations.Add(entity);
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
        return await destination.DayActivations.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
