namespace Icbank.Platform.Application.Gac;

/// <summary>Ports a single row of <c>gac_social_posts</c> (API-SURFACE.md §12).</summary>
/// <param name="Id">The post id.</param>
/// <param name="Platform">The source platform.</param>
/// <param name="ExternalId">The post's id on the originating platform.</param>
/// <param name="ContentAr">The Arabic post content, if any.</param>
/// <param name="ContentEn">The English post content, if any.</param>
/// <param name="PostUrl">The original post URL.</param>
/// <param name="MediaUrl">The attached media URL, if any.</param>
/// <param name="MediaType">The attached media kind.</param>
/// <param name="PostedAt">The UTC timestamp the post was originally published, if known.</param>
/// <param name="Account">The publishing account handle.</param>
public sealed record GacSocialPostDto(
    int Id,
    string Platform,
    string ExternalId,
    string? ContentAr,
    string? ContentEn,
    string PostUrl,
    string? MediaUrl,
    string MediaType,
    DateTimeOffset? PostedAt,
    string Account);
