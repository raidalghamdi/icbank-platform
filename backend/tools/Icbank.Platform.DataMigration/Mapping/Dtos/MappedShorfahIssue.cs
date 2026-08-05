namespace Icbank.Platform.DataMigration.Mapping.Dtos;

/// <summary>Pure DTO produced by <see cref="Transformers.ShorfahIssueTransformer"/>.</summary>
/// <param name="SourceId">The source Postgres <c>shorfah_issues.id</c>.</param>
/// <param name="IssueNo">The sequential issue number (unique in source).</param>
/// <param name="TitleAr">The Arabic title.</param>
/// <param name="SubtitleAr">The optional Arabic subtitle.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Year">The calendar year.</param>
/// <param name="CoverImageUrl">The cover image URL, if any.</param>
/// <param name="EditorLetter">The editor's letter content, if any.</param>
/// <param name="ContributionsOpenAtUtc">The UTC timestamp contributions opened, if set.</param>
/// <param name="ContributionsCloseAtUtc">The UTC timestamp contributions closed, if set.</param>
/// <param name="Status">The issue's workflow status (source free text).</param>
/// <param name="PublishedPdfUrl">The published PDF URL, once published.</param>
/// <param name="PublishedAtUtc">The UTC timestamp of publication, if published.</param>
/// <param name="CreatedBySourceId">The source id of the creating user, if known.</param>
/// <param name="CreatedAtUtc">The resolved (possibly backfilled) row-creation instant.</param>
/// <param name="CreatedAtBackfilled">Whether <paramref name="CreatedAtUtc"/> is synthetic (source <c>created_at</c> is nullable despite <c>defaultNow()</c> — AMBIGUOUS-8).</param>
/// <param name="UpdatedAtUtc">The resolved (possibly backfilled) row-update instant.</param>
/// <param name="UpdatedAtBackfilled">Whether <paramref name="UpdatedAtUtc"/> is synthetic.</param>
public sealed record MappedShorfahIssue(
    int SourceId,
    int IssueNo,
    string TitleAr,
    string? SubtitleAr,
    int Month,
    int Year,
    string? CoverImageUrl,
    string? EditorLetter,
    DateTime? ContributionsOpenAtUtc,
    DateTime? ContributionsCloseAtUtc,
    string Status,
    string? PublishedPdfUrl,
    DateTime? PublishedAtUtc,
    int? CreatedBySourceId,
    DateTime CreatedAtUtc,
    bool CreatedAtBackfilled,
    DateTime UpdatedAtUtc,
    bool UpdatedAtBackfilled);
