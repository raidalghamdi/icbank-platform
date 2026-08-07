using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.Shorfah;

/// <summary>
/// One content section within an issue -- the core contributor workflow unit
/// (DATA-MODEL.md section 3.8 <c>shorfah_sections</c>).
/// </summary>
/// <remarks>
/// Deviation: DATA-MODEL.md flags six unenforced implied FKs on this table (<c>issue_id</c>,
/// <c>parent_section_id</c>, <c>owner_user_id</c>, <c>contributed_by</c>, <c>reviewed_by</c>,
/// <c>approved_by</c>) as "the single biggest FK-integrity gap in the schema". All six are now
/// proper, enforced foreign keys.
/// </remarks>
public sealed class ShorfahSection : AuditableEntity
{
    /// <summary>Gets or sets the owning issue's id.</summary>
    public int IssueId { get; set; }

    /// <summary>Gets or sets the issue navigation property.</summary>
    public ShorfahIssue Issue { get; set; } = null!;

    /// <summary>Gets or sets the parent section's id, for sub-sections.</summary>
    public int? ParentSectionId { get; set; }

    /// <summary>Gets or sets the parent-section navigation property.</summary>
    public ShorfahSection? ParentSection { get; set; }

    /// <summary>Gets or sets the section type.</summary>
    public ShorfahSectionType SectionType { get; set; }

    /// <summary>Gets or sets the Arabic title.</summary>
    public string TitleAr { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional Arabic description.</summary>
    public string? DescriptionAr { get; set; }

    /// <summary>Gets or sets the display sort order.</summary>
    public int DisplayOrder { get; set; }

    /// <summary>Gets or sets the id of the section's owning user, if any.</summary>
    public int? OwnerUserId { get; set; }

    /// <summary>Gets or sets the owning-user navigation property.</summary>
    public User? OwnerUser { get; set; }

    /// <summary>Gets or sets the owning role name, if scoped by role rather than user.</summary>
    public string? OwnerRole { get; set; }

    /// <summary>Gets or sets a value indicating whether the section is included in the published PDF.</summary>
    public bool IncludeInPdf { get; set; } = true;

    /// <summary>Gets or sets a value indicating whether the section content is AI-auto-generated.</summary>
    public bool? AutoGenerate { get; set; }

    /// <summary>Gets or sets a custom AI prompt override for this section.</summary>
    public string? GenerationPrompt { get; set; }

    /// <summary>Gets or sets the contribution workflow status.</summary>
    public ShorfahWorkflowStatus WorkflowStatus { get; set; } = ShorfahWorkflowStatus.PendingContribution;

    /// <summary>Gets or sets the markdown content body.</summary>
    public string? ContentMd { get; set; }

    /// <summary>
    /// Gets or sets the HTML content body. DATA-MODEL.md originally flagged this as a dead write
    /// path (accepted but never rendered) and a latent stored-XSS risk (SEC-11) if a future
    /// refactor rendered it raw. SEC-11 is now closed at the write boundary:
    /// <c>PatchShorfahSectionCommandHandler</c> sanitizes every value assigned here via
    /// <c>IHtmlSanitizer</c> before it reaches this setter, so any future renderer consumes
    /// already-allowlisted markup. Kept nullable/string for source fidelity with the original
    /// schema.
    /// </summary>
    public string? ContentHtml { get; set; }

    /// <summary>Gets or sets the id of the user who contributed, if any.</summary>
    public int? ContributedByUserId { get; set; }

    /// <summary>Gets or sets the contributing-user navigation property.</summary>
    public User? ContributedByUser { get; set; }

    /// <summary>Gets or sets the UTC timestamp of contribution.</summary>
    public DateTimeOffset? ContributedAt { get; set; }

    /// <summary>Gets or sets the id of the user who reviewed, if any.</summary>
    public int? ReviewedByUserId { get; set; }

    /// <summary>Gets or sets the reviewing-user navigation property.</summary>
    public User? ReviewedByUser { get; set; }

    /// <summary>Gets or sets the UTC timestamp of review.</summary>
    public DateTimeOffset? ReviewedAt { get; set; }

    /// <summary>Gets or sets review notes.</summary>
    public string? ReviewNotes { get; set; }

    /// <summary>Gets or sets the id of the user who gave final approval, if any.</summary>
    public int? ApprovedByUserId { get; set; }

    /// <summary>Gets or sets the approving-user navigation property.</summary>
    public User? ApprovedByUser { get; set; }

    /// <summary>Gets or sets the UTC timestamp of approval.</summary>
    public DateTimeOffset? ApprovedAt { get; set; }

    /// <summary>Gets or sets the rejection reason, if rejected.</summary>
    public string? RejectionReason { get; set; }

    /// <summary>Gets or sets the SLA day count for this section.</summary>
    public int? SlaDays { get; set; } = 7;

    /// <summary>Gets or sets the UTC timestamp the SLA clock started.</summary>
    public DateTimeOffset? SlaStartsAt { get; set; }

    /// <summary>
    /// Gets or sets the computed SLA deadline. Computed in application code as
    /// <c>SlaStartsAt + SlaDays</c> in the source system, not a database computed column
    /// (preserved as a plain column here for the same reason).
    /// </summary>
    public DateTimeOffset? SlaDeadline { get; set; }

    /// <summary>Gets the sub-sections of this section.</summary>
    public ICollection<ShorfahSection> ChildSections { get; init; } = new List<ShorfahSection>();

    /// <summary>Gets the permission grants scoped to this section.</summary>
    public ICollection<ShorfahSectionPermission> Permissions { get; init; } = new List<ShorfahSectionPermission>();

    /// <summary>Gets the media attached to this section.</summary>
    public ICollection<ShorfahSectionMedia> Media { get; init; } = new List<ShorfahSectionMedia>();

    /// <summary>Gets the workflow-transition log entries for this section.</summary>
    public ICollection<ShorfahWorkflowLog> WorkflowLogs { get; init; } = new List<ShorfahWorkflowLog>();

    /// <summary>Gets the contributor/role assignments for this section.</summary>
    public ICollection<ShorfahAssignment> Assignments { get; init; } = new List<ShorfahAssignment>();

    /// <summary>Gets the reminder notifications sent for this section.</summary>
    public ICollection<ShorfahReminder> Reminders { get; init; } = new List<ShorfahReminder>();

    /// <summary>Gets the notifications scoped to this section.</summary>
    public ICollection<ShorfahNotification> Notifications { get; init; } = new List<ShorfahNotification>();
}
