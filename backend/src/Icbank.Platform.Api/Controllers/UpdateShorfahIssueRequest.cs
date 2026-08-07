namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PATCH /api/v1/shorfah/issues/{issueId}</c>. Every field is a partial-update field; <c>null</c> means "leave unchanged".</summary>
/// <param name="TitleAr">The replacement title.</param>
/// <param name="SubtitleAr">The replacement subtitle.</param>
/// <param name="EditorLetter">The replacement editor letter.</param>
/// <param name="CoverImageUrl">The replacement cover image URL.</param>
/// <param name="Status">The replacement status.</param>
/// <param name="ContributionsOpenAt">The replacement contributions-open timestamp.</param>
/// <param name="ContributionsCloseAt">The replacement contributions-close timestamp.</param>
public sealed record UpdateShorfahIssueRequest(
    string? TitleAr,
    string? SubtitleAr,
    string? EditorLetter,
    string? CoverImageUrl,
    string? Status,
    DateTimeOffset? ContributionsOpenAt,
    DateTimeOffset? ContributionsCloseAt);
