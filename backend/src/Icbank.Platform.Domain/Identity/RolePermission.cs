using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// The role x page x permission grant matrix (DATA-MODEL.md section 3.1 <c>role_permissions</c>).
/// Unique on (RoleId, PageId, PermissionId) per the source <c>role_page_perm_idx</c> index.
/// </summary>
public sealed class RolePermission : AuditableEntity
{
    /// <summary>Gets or sets the granted role's id.</summary>
    public int RoleId { get; set; }

    /// <summary>Gets or sets the role navigation property.</summary>
    public Role Role { get; set; } = null!;

    /// <summary>Gets or sets the scoped page's id.</summary>
    public int PageId { get; set; }

    /// <summary>Gets or sets the page navigation property.</summary>
    public Page Page { get; set; } = null!;

    /// <summary>Gets or sets the granted permission's id.</summary>
    public int PermissionId { get; set; }

    /// <summary>Gets or sets the permission navigation property.</summary>
    public Permission Permission { get; set; } = null!;
}
