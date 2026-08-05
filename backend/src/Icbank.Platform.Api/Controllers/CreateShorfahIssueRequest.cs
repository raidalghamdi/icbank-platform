namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/shorfah/issues</c>.</summary>
/// <param name="IssueNo">The explicit issue number, or <c>null</c> to auto-assign.</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="SubtitleAr">The optional Arabic subtitle.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Year">The calendar year.</param>
/// <param name="ContributionsOpenAt">The optional UTC timestamp contributions open.</param>
/// <param name="ContributionsCloseAt">The optional UTC timestamp contributions close.</param>
/// <param name="EditorLetter">The optional editor's letter content.</param>
public sealed record CreateShorfahIssueRequest(
    int? IssueNo,
    string TitleAr,
    string? SubtitleAr,
    int Month,
    int Year,
    DateTimeOffset? ContributionsOpenAt,
    DateTimeOffset? ContributionsCloseAt,
    string? EditorLetter);
