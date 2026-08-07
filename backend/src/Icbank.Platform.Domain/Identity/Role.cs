using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// Named role definition (system or custom) that gates page/permission access
/// (DATA-MODEL.md §3.1 <c>roles</c>).
/// </summary>
public sealed class Role : AuditableEntity
{
    /// <summary>Gets or sets the machine name of the role, e.g. <c>super_admin</c>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the Arabic display label.</summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>Gets or sets an optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets a value indicating whether this is a system role that cannot be deleted.</summary>
    public bool IsSystem { get; set; }

    /// <summary>Gets the role-permission grants for this role.</summary>
    public ICollection<RolePermission> RolePermissions { get; init; } = new List<RolePermission>();

    /// <summary>Gets the user assignments for this role.</summary>
    public ICollection<UserRole> UserRoles { get; init; } = new List<UserRole>();
}
