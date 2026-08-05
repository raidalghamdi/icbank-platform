using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.Weekend;

/// <summary>
/// AI-generated weekly "weekend content" draft (places/deals/podcasts/matches/movies bundle)
/// (DATA-MODEL.md section 3.10 <c>weekend_drafts</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>generated_by</c> and <c>approved_by</c> were unenforced implied FKs in the
/// source schema; both are now proper, enforced, optional foreign keys.
/// </remarks>
public sealed class WeekendDraft : AuditableEntity
{
    /// <summary>
    /// Gets or sets the ISO date string of the target Thursday. Kept as text to match source
    /// fidelity (DATA-MODEL.md notes this is not a real <c>date</c> column in the source).
    /// </summary>
    public string WeekendDate { get; set; } = string.Empty;

    /// <summary>Gets or sets the city, hardcoded to Riyadh by default in the source system.</summary>
    public string City { get; set; } = "الرياض";

    /// <summary>Gets or sets the review workflow status.</summary>
    public WeekendDraftStatus Status { get; set; } = WeekendDraftStatus.PendingReview;

    /// <summary>Gets or sets the generating model name.</summary>
    public string ModelName { get; set; } = "gemini-2.0-flash-exp";

    /// <summary>
    /// Gets or sets the fully untyped content payload as JSON text (DATA-MODEL.md section 6).
    /// Actual shape: <c>{summary, places[], deals[], podcasts[], matches[], movies[]}</c>.
    /// </summary>
    public string ContentJson { get; set; } = "{}";

    /// <summary>Gets or sets the id of the user who generated this draft, if known.</summary>
    public int? GeneratedByUserId { get; set; }

    /// <summary>Gets or sets the generating-user navigation property.</summary>
    public User? GeneratedByUser { get; set; }

    /// <summary>Gets or sets the id of the user who approved this draft, if any.</summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>Gets or sets the approving-user navigation property.</summary>
    public User? ApprovedByUser { get; set; }

    /// <summary>Gets or sets the rejection reason, if rejected.</summary>
    public string? RejectedReason { get; set; }

    /// <summary>Gets or sets the UTC timestamp of approval.</summary>
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>Gets or sets the UTC timestamp of publication.</summary>
    public DateTimeOffset? PublishedAt { get; set; }
}
