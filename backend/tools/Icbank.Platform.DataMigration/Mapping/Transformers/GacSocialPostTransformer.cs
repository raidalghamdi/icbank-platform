using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Validation;

namespace Icbank.Platform.DataMigration.Mapping.Transformers;

/// <summary>
/// Pure transformer from a raw <c>gac_social_posts</c> row to <see cref="MappedGacSocialPost"/>.
/// Does not itself decide what to do with duplicate (platform, external_id) pairs — that
/// detection is the job of <see cref="DuplicateKeyDetector"/>, run across the whole mapped set,
/// so this transformer stays a simple one-row-in one-row-out function (task requirement 3/4:
/// detect and report duplicates rather than crash).
/// </summary>
public static class GacSocialPostTransformer
{
    /// <summary>Transforms one raw <c>gac_social_posts</c> row.</summary>
    /// <param name="row">The raw source row.</param>
    /// <returns>The mapped, destination-ready DTO.</returns>
    public static MappedGacSocialPost Transform(SourceRow row)
    {
        DateTime createdAtRaw = row.GetRawTimestamp("created_at")
            ?? throw new InvalidOperationException("gac_social_posts.created_at was null.");

        return new MappedGacSocialPost(
            SourceId: row.GetInt32("id"),
            Platform: row.GetString("platform"),
            ExternalId: row.GetString("external_id"),
            ContentAr: row.GetNullableString("content_ar"),
            ContentEn: row.GetNullableString("content_en"),
            PostUrl: row.GetString("post_url"),
            MediaUrl: row.GetNullableString("media_url"),
            MediaType: string.IsNullOrEmpty(row.GetNullableString("media_type")) ? "None" : row.GetString("media_type"),
            PostedAt: TimestampConverter.ToDestinationOffset(row.GetRawTimestamp("posted_at")),
            LikeCount: row.GetNullableInt32("likes"),
            CommentCount: row.GetNullableInt32("comments"),
            ShareCount: row.GetNullableInt32("shares"),
            Account: row.GetString("account"),
            CreatedAtUtc: createdAtRaw);
    }
}
