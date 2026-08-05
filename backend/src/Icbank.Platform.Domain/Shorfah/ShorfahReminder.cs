using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.Shorfah;

/// <summary>
/// Log of every reminder notification sent (initial/overdue/pre-due)
/// (DATA-MODEL.md section 3.8 <c>shorfah_reminders</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>section_id</c>, <c>assignment_id</c>, and <c>recipient_user_id</c> were all
/// unenforced implied FKs; all three are now proper, enforced foreign keys.
/// </remarks>
public sealed class ShorfahReminder : AuditableEntity
{
    /// <summary>Gets or sets the owning section's id.</summary>
    public int SectionId { get; set; }

    /// <summary>Gets or sets the section navigation property.</summary>
    public ShorfahSection Section { get; set; } = null!;

    /// <summary>Gets or sets the related assignment's id, if any.</summary>
    public int? AssignmentId { get; set; }

    /// <summary>Gets or sets the assignment navigation property.</summary>
    public ShorfahAssignment? Assignment { get; set; }

    /// <summary>Gets or sets the recipient user's id.</summary>
    public int RecipientUserId { get; set; }

    /// <summary>Gets or sets the recipient navigation property.</summary>
    public User RecipientUser { get; set; } = null!;

    /// <summary>Gets or sets the delivery channel.</summary>
    public ShorfahReminderChannel Channel { get; set; }

    /// <summary>Gets or sets the reminder kind.</summary>
    public ShorfahReminderType ReminderType { get; set; }

    /// <summary>Gets or sets the UTC timestamp the reminder was sent.</summary>
    public DateTimeOffset? SentAt { get; set; }

    /// <summary>Gets or sets the delivery status.</summary>
    public string? Status { get; set; } = "sent";

    /// <summary>Gets or sets the rendered message body.</summary>
    public string? Message { get; set; }
}
