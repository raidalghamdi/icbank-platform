using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// Per-user allow/deny override on top of role-based permissions
/// (DATA-MODEL.md section 3.1 <c>user_page_overrides</c>).
/// </summary>
public sealed class UserPageOverride : AuditableEntity
{
    /// <summary>Gets or sets the overridden user's id.</summary>
    public int UserId { get; set; }

    /// <summary>Gets or sets the user navigation property.</summary>
    public User User { get; set; } = null!;

    /// <summary>Gets or sets the scoped page's id.</summary>
    public int PageId { get; set; }

    /// <summary>Gets or sets the page navigation property.</summary>
    public Page Page { get; set; } = null!;

    /// <summary>Gets or sets the scoped permission's id.</summary>
    public int PermissionId { get; set; }

    /// <summary>Gets or sets the permission navigation property.</summary>
    public Permission Permission { get; set; } = null!;

    /// <summary>Gets or sets the grant kind: allow or deny.</summary>
    public OverrideGrantType GrantType { get; set; }

    /// <summary>Gets or sets the id of the user who created the override, if known.</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>Gets or sets the creating-user navigation property.</summary>
    public User? CreatedByUser { get; set; }
}
