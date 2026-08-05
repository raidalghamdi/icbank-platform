namespace Icbank.Platform.DataMigration.Mapping.Dtos;

/// <summary>Pure DTO produced by <see cref="Transformers.GacSocialPostTransformer"/>.</summary>
/// <param name="SourceId">The source Postgres <c>gac_social_posts.id</c>.</param>
/// <param name="Platform">The social platform name.</param>
/// <param name="ExternalId">The post's id on the originating platform (part of the new unique key).</param>
/// <param name="ContentAr">The Arabic content.</param>
/// <param name="ContentEn">The English content.</param>
/// <param name="PostUrl">The original post URL.</param>
/// <param name="MediaUrl">The attached media URL, if any.</param>
/// <param name="MediaType">The attached media kind.</param>
/// <param name="PostedAt">The original publish instant, converted to UTC-based <see cref="DateTimeOffset"/>.</param>
/// <param name="LikeCount">The like count, if known.</param>
/// <param name="CommentCount">The comment count, if known.</param>
/// <param name="ShareCount">The share count, if known.</param>
/// <param name="Account">The publishing account handle.</param>
/// <param name="CreatedAtUtc">The original row-creation instant.</param>
public sealed record MappedGacSocialPost(
    int SourceId,
    string Platform,
    string ExternalId,
    string? ContentAr,
    string? ContentEn,
    string PostUrl,
    string? MediaUrl,
    string MediaType,
    DateTimeOffset? PostedAt,
    int? LikeCount,
    int? CommentCount,
    int? ShareCount,
    string Account,
    DateTime CreatedAtUtc)
{
    /// <summary>Gets the new unique-index key this row will occupy in the destination (<c>platform</c>, <c>external_id</c>).</summary>
    public (string Platform, string ExternalId) UniqueKey => (Platform, ExternalId);
}
