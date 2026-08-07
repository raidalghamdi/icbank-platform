using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// User to role assignment (DATA-MODEL.md section 3.1 <c>user_roles</c>). The schema supports
/// many-to-many, but the application only ever reads the first row for a user (AMBIGUOUS-3 in
/// DATA-MODEL.md) -- flagged for product review, see DOMAIN-PORT-NOTES.md.
/// </summary>
public sealed class UserRole : AuditableEntity
{
    /// <summary>Gets or sets the assigned user's id.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the user navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the assigned role's id.</summary>
    public int RoleId { get; set; }

    /// <summary>Gets or sets the role navigation property.</summary>
    public Role Role { get; set; } = null!;

    /// <summary>Gets or sets the id of the user who performed the assignment, if known.</summary>
    public int? AssignedById { get; set; }

    /// <summary>Gets or sets the assigning-user navigation property.</summary>
    public User? AssignedBy { get; set; }

    /// <summary>Gets or sets the UTC timestamp of the assignment.</summary>
    public DateTime AssignedAt { get; set; }
}
