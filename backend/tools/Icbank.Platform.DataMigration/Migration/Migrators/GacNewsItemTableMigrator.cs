using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>Migrates <c>gac_news_items</c> → <see cref="GacNewsItem"/>.</summary>
public sealed class GacNewsItemTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "gac_news_items";

    /// <inheritdoc />
    public string DestinationTableName => "gac_news_items";

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
            var category = row.GetNullableString("category");

            var entity = new GacNewsItem
            {
                Kind = SnakeCaseEnumParser.Parse<GacNewsKind>(
                    string.IsNullOrEmpty(row.GetNullableString("kind")) ? "news" : row.GetString("kind")),
                TitleAr = row.GetString("title_ar"),
                TitleEn = row.GetNullableString("title_en"),
                BodyAr = row.GetNullableString("body_ar"),
                BodyEn = row.GetNullableString("body_en"),
                Category = string.IsNullOrEmpty(category) ? null : SnakeCaseEnumParser.Parse<GacNewsCategory>(category),
                SourceUrl = row.GetNullableString("source_url"),
                ImageUrl = row.GetNullableString("image_url"),
                PublishedAt = publishedAtRaw.HasValue ? new DateTimeOffset(publishedAtRaw.Value, TimeSpan.Zero) : null,
                ExternalRef = row.GetNullableString("external_ref"),
                Tags = row.GetStringArray("tags").ToList(),
                CreatedAt = createdAt,
                CreatedBy = "data-migration-tool",
            };

            destination.GacNewsItems.Add(entity);
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
        return await destination.GacNewsItems.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
