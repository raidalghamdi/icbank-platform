using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Weekend;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>style_profile</c> → <see cref="StyleProfile"/>. The source schema treats this as
/// an application-level singleton (no DB constraint enforces exactly one row); this migrator
/// carries across every source row as-is rather than imposing a uniqueness rule the source never
/// had, consistent with the domain entity's documented decision.
/// </summary>
public sealed class StyleProfileTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "style_profile";

    /// <inheritdoc />
    public string DestinationTableName => "style_profile";

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

            // style_profile has no created_at column in the source schema (only updated_at);
            // CreatedAt is set to the migration-run timestamp, a synthetic value with no
            // backfill-priority reasoning applied (same rationale as ShorfahReminder).
            DateTime migrationRunTimestamp = context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new StyleProfile
            {
                ToneSummary = row.GetNullableString("tone_summary"),
                AvgParagraphLength = row.GetNullableFloat("avg_paragraph_length"),
                OpenerPatterns = row.GetStringArray("opener_patterns").ToList(),
                CloserPatterns = row.GetStringArray("closer_patterns").ToList(),
                RecurringKeywords = row.GetStringArray("recurring_keywords").ToList(),
                QuoteUsage = row.GetNullableString("quote_usage"),
                CreatedAt = migrationRunTimestamp,
                CreatedBy = "data-migration-tool",
                UpdatedAt = row.GetRawTimestamp("updated_at"),
            };

            destination.StyleProfiles.Add(entity);
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
        return await destination.StyleProfiles.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
