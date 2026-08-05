using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Weekend;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>generated_outputs</c> (AI week-start draft candidates) → <see cref="GeneratedOutput"/>.</summary>
/// <remarks>
/// <c>archive_refs</c> is a <c>jsonb number[]</c> of implied, unenforced <see cref="ArchiveEntry"/>
/// source ids. Values are re-pointed through the id-mapping store so they reference the migrated
/// destination <see cref="ArchiveEntry"/> ids rather than the stale source ids; a source id with
/// no corresponding mapping (i.e. its <c>archive_entries</c> row was not migrated or does not
/// exist) is dropped rather than left dangling, since the destination schema does not enforce
/// this as a real foreign key either way.
/// </remarks>
public sealed class GeneratedOutputTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "generated_outputs";

    /// <inheritdoc />
    public string DestinationTableName => "generated_outputs";

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

            var archiveRefIds = new List<int>();
            foreach (var sourceArchiveId in row.GetInt32Array("archive_refs"))
            {
                var mappedArchiveId = await context.IdMap.TryGetDestinationIdAsync("archive_entries", sourceArchiveId, cancellationToken);
                if (mappedArchiveId.HasValue)
                {
                    archiveRefIds.Add(mappedArchiveId.Value);
                }
            }

            var entity = new GeneratedOutput
            {
                Topic = row.GetString("topic"),
                ModelName = row.GetString("model_name"),
                OutputText = row.GetString("output_text"),
                ArchiveRefIds = archiveRefIds,
                Selected = row.GetBoolean("selected"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.GeneratedOutputs.Add(entity);
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
        return await destination.GeneratedOutputs.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
