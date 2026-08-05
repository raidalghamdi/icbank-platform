using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.Shorfah;

/// <summary>In-app notification inbox per user (DATA-MODEL.md section 3.8 <c>shorfah_notifications</c>).</summary>
/// <remarks>Deviation: <c>user_id</c>, <c>issue_id</c>, and <c>section_id</c> were all unenforced implied FKs; all three are now proper, enforced foreign keys.</remarks>
public sealed class ShorfahNotification : AuditableEntity
{
    /// <summary>Gets or sets the recipient user's id.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the recipient navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the related issue's id, if any.</summary>
    public int? IssueId { get; set; }

    /// <summary>Gets or sets the issue navigation property.</summary>
    public ShorfahIssue? Issue { get; set; }

    /// <summary>Gets or sets the related section's id, if any.</summary>
    public int? SectionId { get; set; }

    /// <summary>Gets or sets the section navigation property.</summary>
    public ShorfahSection? Section { get; set; }

    /// <summary>Gets or sets the notification type, e.g. "initial", "reminder_overdue", "published".</summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the notification body.</summary>
    public string? Body { get; set; }

    /// <summary>Gets or sets a relative in-app URL to navigate to.</summary>
    public string? Url { get; set; }

    /// <summary>Gets or sets a value indicating whether the notification has been read.</summary>
    public bool? IsRead { get; set; }
}
