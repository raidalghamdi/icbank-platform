using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>Catalogue of the five action verbs (DATA-MODEL.md §3.1 <c>permissions</c>).</summary>
public sealed class Permission : AuditableEntity
{
    /// <summary>Gets or sets the machine name, one of <see cref="PermissionVerbName"/>.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the Arabic display label.</summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>Gets the role-permission grants using this permission.</summary>
    public ICollection<RolePermission> RolePermissions { get; init; } = new List<RolePermission>();

    /// <summary>Gets the user overrides using this permission.</summary>
    public ICollection<UserPageOverride> UserPageOverrides { get; init; } = new List<UserPageOverride>();
}
