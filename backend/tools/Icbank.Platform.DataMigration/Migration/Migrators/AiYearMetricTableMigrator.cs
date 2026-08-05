using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>ai_year_metrics</c> → <see cref="AiYearMetric"/>.</summary>
public sealed class AiYearMetricTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "ai_year_metrics";

    /// <inheritdoc />
    public string DestinationTableName => "ai_year_metrics";

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

            var activationId = await context.IdMap.TryGetDestinationIdAsync("ai_year_activations", row.GetInt32("activation_id"), cancellationToken);
            if (!activationId.HasValue)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"Source row id={sourceId} references activation_id that was not migrated; skipped.");
                continue;
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new AiYearMetric
            {
                ActivationId = activationId.Value,
                MetricKey = row.GetString("metric_key"),
                MetricValue = row.GetNullableString("metric_value"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.AiYearMetrics.Add(entity);
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
        return await destination.AiYearMetrics.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
