using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.Shorfah;

/// <summary>Contributor/role assignment per section (DATA-MODEL.md section 3.8 <c>shorfah_assignments</c>).</summary>
/// <remarks>Deviation: <c>section_id</c> and <c>user_id</c> were unenforced implied FKs; both are now proper, enforced foreign keys.</remarks>
public sealed class ShorfahAssignment : AuditableEntity
{
    /// <summary>Gets or sets the owning section's id.</summary>
    public int SectionId { get; set; }

    /// <summary>Gets or sets the section navigation property.</summary>
    public ShorfahSection Section { get; set; } = null!;

    /// <summary>Gets or sets the assigned user's id.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the user navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the assignment role label, e.g. "contributor".</summary>
    public string? Role { get; set; } = "contributor";

    /// <summary>Gets the reminders sent for this assignment.</summary>
    public ICollection<ShorfahReminder> Reminders { get; init; } = new List<ShorfahReminder>();
}
