using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.Shorfah;

/// <summary>
/// One monthly magazine issue -- the top-level workflow container
/// (DATA-MODEL.md section 3.8 <c>shorfah_issues</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>created_by</c> was an unenforced implied FK in the source schema
/// (DATA-MODEL.md section 4). It is now a proper, enforced, optional foreign key.
/// Deviation: source <c>created_at</c>/<c>updated_at</c> were nullable despite having
/// <c>defaultNow()</c> (AMBIGUOUS-8 in DATA-MODEL.md). This port makes them non-null via the
/// shared <see cref="AuditableEntity"/> base, resolving the ambiguity in favor of consistency
/// with the rest of the schema -- flagged for product review in DOMAIN-PORT-NOTES.md.
/// </remarks>
public sealed class ShorfahIssue : AuditableEntity
{
    /// <summary>Gets or sets the sequential issue number.</summary>
    public int IssueNo { get; set; }

    /// <summary>Gets or sets the Arabic title.</summary>
    public string TitleAr { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional Arabic subtitle.</summary>
    public string? SubtitleAr { get; set; }

    /// <summary>Gets or sets the calendar month (1-12).</summary>
    public int Month { get; set; }

    /// <summary>Gets or sets the calendar year.</summary>
    public int Year { get; set; }

    /// <summary>Gets or sets the cover image URL.</summary>
    public string? CoverImageUrl { get; set; }

    /// <summary>Gets or sets the editor's letter content.</summary>
    public string? EditorLetter { get; set; }

    /// <summary>Gets or sets the UTC timestamp contributions open.</summary>
    public DateTimeOffset? ContributionsOpenAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp contributions close.</summary>
    public DateTimeOffset? ContributionsCloseAt { get; set; }

    /// <summary>Gets or sets the issue's workflow status.</summary>
    public ShorfahIssueStatus Status { get; set; } = ShorfahIssueStatus.Collecting;

    /// <summary>Gets or sets the published PDF URL, once published.</summary>
    public string? PublishedPdfUrl { get; set; }

    /// <summary>Gets or sets the UTC timestamp of publication.</summary>
    public DateTimeOffset? PublishedAt { get; set; }

    /// <summary>Gets or sets the id of the user who created this issue, if known.</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>Gets or sets the creating-user navigation property.</summary>
    public User? CreatedByUser { get; set; }

    /// <summary>Gets the content sections belonging to this issue.</summary>
    public ICollection<ShorfahSection> Sections { get; init; } = new List<ShorfahSection>();

    /// <summary>Gets the notifications scoped to this issue.</summary>
    public ICollection<ShorfahNotification> Notifications { get; init; } = new List<ShorfahNotification>();
}
