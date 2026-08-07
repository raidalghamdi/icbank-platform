using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.Shorfah;

/// <summary>
/// Per-section, per-user-or-role grant of contribute/review/approve/view
/// (DATA-MODEL.md section 3.8 <c>shorfah_section_permissions</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>section_id</c> and <c>user_id</c> were both unenforced implied FKs in the
/// source schema. Both are now proper, enforced foreign keys (the latter remains optional since
/// a grant may instead target <see cref="RoleName"/>).
/// </remarks>
public sealed class ShorfahSectionPermission : AuditableEntity
{
    /// <summary>Gets or sets the scoped section's id.</summary>
    public int SectionId { get; set; }

    /// <summary>Gets or sets the section navigation property.</summary>
    public ShorfahSection Section { get; set; } = null!;

    /// <summary>Gets or sets the granted user's id, mutually exclusive with <see cref="RoleName"/> by convention.</summary>
    public int? UserId { get; set; }

    /// <summary>Gets or sets the granted-user navigation property.</summary>
    public Identity.User? User { get; set; }

    /// <summary>Gets or sets the granted role name, mutually exclusive with <see cref="UserId"/> by convention.</summary>
    public string? RoleName { get; set; }

    /// <summary>Gets or sets the granted permission verb.</summary>
    public ShorfahPermissionVerb Permission { get; set; }
}
