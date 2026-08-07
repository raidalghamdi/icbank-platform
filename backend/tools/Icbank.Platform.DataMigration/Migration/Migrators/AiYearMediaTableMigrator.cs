using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.AiYear;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>ai_year_media</c> → <see cref="AiYearMedia"/>.</summary>
/// <remarks>
/// <c>activation_id</c> is a required, cascading FK to <c>ai_year_activations</c>, which the
/// registry runs before this migrator. A source row whose activation was not migrated is skipped
/// and recorded as a data issue rather than silently dropped or inserted with a bad id.
/// </remarks>
public sealed class AiYearMediaTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "ai_year_media";

    /// <inheritdoc />
    public string DestinationTableName => "ai_year_media";

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

            var entity = new AiYearMedia
            {
                ActivationId = activationId.Value,
                ObjectPath = row.GetString("object_path"),
                FileName = row.GetNullableString("file_name"),
                ContentType = row.GetNullableString("content_type"),
                SortOrder = row.GetNullableInt32("sort_order") ?? 0,
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.AiYearMedia.Add(entity);
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
        return await destination.AiYearMedia.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
