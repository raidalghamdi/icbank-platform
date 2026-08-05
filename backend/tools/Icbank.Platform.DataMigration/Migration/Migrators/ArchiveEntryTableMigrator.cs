using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Weekend;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>archive_entries</c> (week-start message archive/RAG source) → <see cref="ArchiveEntry"/>.
/// </summary>
/// <remarks>
/// The source <c>embedding</c> column is a raw <c>jsonb</c> float array with no SQL Server
/// pgvector equivalent; it is carried across as a plain JSON-backed float list, matching the
/// domain entity's own documented "no vector store migration performed" decision.
/// </remarks>
public sealed class ArchiveEntryTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "archive_entries";

    /// <inheritdoc />
    public string DestinationTableName => "archive_entries";

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
            DateTime? date = row.GetRawTimestamp("date");

            var entity = new ArchiveEntry
            {
                Title = row.GetString("title"),
                BodyText = row.GetString("body_text"),
                Date = date.HasValue ? new DateTimeOffset(date.Value, TimeSpan.Zero) : null,
                Occasion = row.GetNullableString("occasion"),
                Tone = row.GetNullableString("tone"),
                SourceFile = row.GetNullableString("source_file"),
                Embedding = row.GetFloatArray("embedding").ToList(),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.ArchiveEntries.Add(entity);
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
        return await destination.ArchiveEntries.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
