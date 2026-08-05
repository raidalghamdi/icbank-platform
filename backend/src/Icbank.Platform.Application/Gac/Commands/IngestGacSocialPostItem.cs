namespace Icbank.Platform.Application.Gac.Commands;

/// <summary>One social post to upsert.</summary>
/// <param name="Platform">The source platform.</param>
/// <param name="ExternalId">The post's id on the originating platform.</param>
/// <param name="ContentAr">The Arabic content, if any.</param>
/// <param name="ContentEn">The English content, if any.</param>
/// <param name="PostUrl">The original post URL.</param>
/// <param name="MediaUrl">The attached media URL, if any.</param>
/// <param name="MediaType">The attached media kind, if any.</param>
/// <param name="PostedAt">The UTC timestamp originally published, if known.</param>
/// <param name="Account">The publishing account handle.</param>
public sealed record IngestGacSocialPostItem(
    string Platform,
    string ExternalId,
    string? ContentAr,
    string? ContentEn,
    string? PostUrl,
    string? MediaUrl,
    string? MediaType,
    DateTimeOffset? PostedAt,
    string? Account);
