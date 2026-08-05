using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Validation;
using Icbank.Platform.Domain.Gac;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Migrates <c>gac_social_posts</c> → <see cref="GacSocialPost"/>. Detects rows that would
/// violate the new <c>ux_gac_social_posts_platform_external_id</c> unique index (AMBIGUOUS-7 /
/// task requirement 3) and reports them instead of letting the insert throw: for each duplicate
/// group, the earliest-created row (by source <c>created_at</c>) is migrated and every other row
/// in the group is skipped and named in the report, so a human can decide whether to
/// reconcile/merge the skipped rows later.
/// </summary>
public sealed class GacSocialPostTableMigrator : ITableMigrator
{
    /// <inheritdoc />
    public string SourceTableName => "gac_social_posts";

    /// <inheritdoc />
    public string DestinationTableName => "gac_social_posts";

    /// <inheritdoc />
    public async Task<TableMigrationResult> MigrateAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        var result = new TableMigrationResult { SourceTableName = SourceTableName };
        DateTimeOffset startedAt = context.DateTimeProvider.UtcNow;

        var mappedRows = new List<MappedGacSocialPost>();
        await foreach (SourceRow row in context.Source.ReadTableAsync(SourceTableName, cancellationToken))
        {
            result.RowsRead++;
            mappedRows.Add(GacSocialPostTransformer.Transform(row));
        }

        IReadOnlyList<DuplicateKeyGroup<(string Platform, string ExternalId)>> duplicates =
            DuplicateKeyDetector.FindDuplicates(mappedRows, m => m.UniqueKey, m => m.SourceId);

        var sourceIdsToSkip = new HashSet<int>();
        foreach (DuplicateKeyGroup<(string Platform, string ExternalId)> group in duplicates)
        {
            int keepSourceId = group.SourceIds
                .Select(id => mappedRows.First(m => m.SourceId == id))
                .OrderBy(m => m.CreatedAtUtc)
                .First()
                .SourceId;

            IEnumerable<int> skipped = group.SourceIds.Where(id => id != keepSourceId);
            foreach (int id in skipped)
            {
                sourceIdsToSkip.Add(id);
            }

            result.Notes.Add(
                $"Duplicate key (platform={group.Key.Platform}, external_id={group.Key.ExternalId}): " +
                $"source ids [{string.Join(", ", group.SourceIds)}] — kept earliest-created id {keepSourceId}, " +
                $"skipped [{string.Join(", ", skipped)}] to satisfy the new unique index.");
        }

        await using AppDbContext destination = context.CreateDestinationContext();

        foreach (MappedGacSocialPost mapped in mappedRows)
        {
            if (sourceIdsToSkip.Contains(mapped.SourceId))
            {
                result.RowsSkippedDueToDataIssue++;
                continue;
            }

            int? existingId = await context.IdMap.TryGetDestinationIdAsync(SourceTableName, mapped.SourceId, cancellationToken);
            if (existingId.HasValue)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            bool alreadyExists = await destination.GacSocialPosts.IgnoreQueryFilters()
                .AnyAsync(p => p.Platform.ToString() == mapped.Platform && p.ExternalId == mapped.ExternalId, cancellationToken);
            if (alreadyExists)
            {
                result.RowsSkippedAlreadyMigrated++;
                continue;
            }

            var entity = new GacSocialPost
            {
                Platform = Enum.Parse<GacSocialPlatform>(mapped.Platform, ignoreCase: true),
                ExternalId = mapped.ExternalId,
                ContentAr = mapped.ContentAr,
                ContentEn = mapped.ContentEn,
                PostUrl = mapped.PostUrl,
                MediaUrl = mapped.MediaUrl,
                MediaType = Enum.Parse<GacSocialMediaType>(mapped.MediaType, ignoreCase: true),
                PostedAt = mapped.PostedAt,
                Account = mapped.Account,
                Metrics = new SocialMetrics
                {
                    Likes = mapped.LikeCount,
                    Comments = mapped.CommentCount,
                    Shares = mapped.ShareCount,
                },
                CreatedAt = mapped.CreatedAtUtc,
                CreatedBy = "data-migration-tool",
            };

            destination.GacSocialPosts.Add(entity);
            await destination.SaveChangesAsync(cancellationToken);

            await context.IdMap.RecordAsync(SourceTableName, mapped.SourceId, entity.Id, context.DateTimeProvider.UtcNow, cancellationToken);
            result.RowsInserted++;
        }

        result.Duration = context.DateTimeProvider.UtcNow - startedAt;
        return result;
    }

    /// <inheritdoc />
    public async Task<long> CountDestinationRowsAsync(MigrationRunContext context, CancellationToken cancellationToken)
    {
        await using AppDbContext destination = context.CreateDestinationContext();
        return await destination.GacSocialPosts.IgnoreQueryFilters().LongCountAsync(cancellationToken);
    }
}
