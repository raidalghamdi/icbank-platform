using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>Catalogue of RBAC-gated app sections/pages (DATA-MODEL.md §3.1 <c>pages</c>).</summary>
public sealed class Page : AuditableEntity
{
    /// <summary>Gets or sets the unique slug, e.g. <c>dashboard</c>, <c>shorfah</c>.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Gets or sets the Arabic display label.</summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>Gets or sets the icon identifier, if any.</summary>
    public string? Icon { get; set; }

    /// <summary>Gets or sets the sidebar sort order.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets a value indicating whether the page is active/visible.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets the role-permission grants scoped to this page.</summary>
    public ICollection<RolePermission> RolePermissions { get; init; } = new List<RolePermission>();

    /// <summary>Gets the user overrides scoped to this page.</summary>
    public ICollection<UserPageOverride> UserPageOverrides { get; init; } = new List<UserPageOverride>();
}
