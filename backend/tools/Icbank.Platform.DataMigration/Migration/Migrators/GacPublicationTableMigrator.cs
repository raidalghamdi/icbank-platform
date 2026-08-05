using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>gac_publications</c> → <see cref="GacPublication"/>.</summary>
public sealed class GacPublicationTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "gac_publications";

    /// <inheritdoc />
    public string DestinationTableName => "gac_publications";

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
            DateTime? publishedAtRaw = row.GetRawTimestamp("published_at");

            var entity = new GacPublication
            {
                TitleAr = row.GetString("title_ar"),
                TitleEn = row.GetNullableString("title_en"),
                Category = SnakeCaseEnumParser.Parse<GacPublicationCategory>(row.GetString("category")),
                Language = SnakeCaseEnumParser.Parse<GacPublicationLanguage>(
                    string.IsNullOrEmpty(row.GetNullableString("language")) ? "ar" : row.GetString("language")),
                DescriptionAr = row.GetNullableString("description_ar"),
                DescriptionEn = row.GetNullableString("description_en"),
                Version = row.GetNullableString("version"),
                PublishedAt = publishedAtRaw.HasValue ? new DateTimeOffset(publishedAtRaw.Value, TimeSpan.Zero) : null,
                OriginalUrl = row.GetNullableString("original_url"),
                FileUrl = row.GetString("file_url"),
                FileSizeBytes = row.GetNullableInt32("file_size_bytes"),
                PageCount = row.GetNullableInt32("page_count"),
                Tags = row.GetStringArray("tags").ToList(),
                SourceDomain = SnakeCaseEnumParser.Parse<GacPublicationSourceDomain>(
                    string.IsNullOrEmpty(row.GetNullableString("source_domain")) ? "gacbep" : row.GetString("source_domain")),
                Status = SnakeCaseEnumParser.Parse<GacPublicationStatus>(
                    string.IsNullOrEmpty(row.GetNullableString("status")) ? "published" : row.GetString("status")),
                DisplayOrder = row.GetNullableInt32("display_order") ?? 100,
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
                UpdatedAt = row.GetRawTimestamp("updated_at"),
            };

            destination.GacPublications.Add(entity);
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
        return await destination.GacPublications.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
