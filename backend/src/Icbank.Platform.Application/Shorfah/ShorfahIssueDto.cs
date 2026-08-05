namespace Icbank.Platform.Application.Shorfah;

/// <summary>The Shorfah issue response shape (API-SURFACE.md §19, BUSINESS-RULES.md §1.1).</summary>
/// <param name="Id">The issue id.</param>
/// <param name="IssueNo">The sequential issue number.</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="SubtitleAr">The optional Arabic subtitle.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Year">The calendar year.</param>
/// <param name="CoverImageUrl">The cover image URL, if set.</param>
/// <param name="EditorLetter">The editor's letter content, if set.</param>
/// <param name="ContributionsOpenAt">The UTC timestamp contributions open.</param>
/// <param name="ContributionsCloseAt">The UTC timestamp contributions close.</param>
/// <param name="Status">The issue's workflow status.</param>
/// <param name="PublishedPdfUrl">The published PDF URL, once published.</param>
/// <param name="PublishedAt">The UTC timestamp of publication.</param>
/// <param name="CreatedByUserId">The id of the user who created the issue, if known.</param>
public sealed record ShorfahIssueDto(
    int Id,
    int IssueNo,
    string TitleAr,
    string? SubtitleAr,
    int Month,
    int Year,
    string? CoverImageUrl,
    string? EditorLetter,
    DateTimeOffset? ContributionsOpenAt,
    DateTimeOffset? ContributionsCloseAt,
    string Status,
    string? PublishedPdfUrl,
    DateTimeOffset? PublishedAt,
    int? CreatedByUserId);
