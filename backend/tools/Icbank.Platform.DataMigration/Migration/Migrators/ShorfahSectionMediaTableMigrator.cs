using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>shorfah_section_media</c> → <see cref="ShorfahSectionMedia"/>. Depends on
/// <see cref="ShorfahSectionTableMigrator"/> (required <c>section_id</c> FK).
/// </summary>
public sealed class ShorfahSectionMediaTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "shorfah_section_media";

    /// <inheritdoc />
    public string DestinationTableName => "shorfah_section_media";

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

            var sourceSectionId = row.GetInt32("section_id");
            var sectionId = await context.IdMap.TryGetDestinationIdAsync("shorfah_sections", sourceSectionId, cancellationToken);
            if (sectionId is null)
            {
                result.RowsSkippedDueToDataIssue++;
                result.Notes.Add($"shorfah_section_media source id {sourceId}: orphaned section_id {sourceSectionId} — skipped.");
                continue;
            }

            DateTime createdAt = row.GetRawTimestamp("created_at") ?? context.DateTimeProvider.UtcNow.UtcDateTime;

            var entity = new ShorfahSectionMedia
            {
                SectionId = sectionId.Value,
                MediaUrl = row.GetString("media_url"),
                MediaType = SnakeCaseEnumParser.Parse<ShorfahMediaType>(row.GetString("media_type")),
                CaptionAr = row.GetNullableString("caption_ar"),
                DisplayOrder = row.GetNullableInt32("display_order"),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.ShorfahSectionMedia.Add(entity);
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
        return await destination.ShorfahSectionMedia.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
