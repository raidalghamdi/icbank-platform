namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>The result of resolving a user's effective permissions.</summary>
/// <param name="RoleNames">The union of role machine-names the user holds (may be more than one — see DOMAIN-PORT-NOTES.md multi-role fix).</param>
/// <param name="IsSuperAdmin">Whether the user holds the distinct super-admin capability (closes SEC-01 — never true for plain <c>admin</c>).</param>
/// <param name="Permissions">The effective <c>{pageSlug}:{verb}</c> permission strings.</param>
/// <param name="AccessGrantedBy">
/// The display name of the administrator who most recently created a per-user override for this
/// user, or <c>null</c> when the user's access comes purely from their roles and nobody has
/// tailored it individually. This is presentational only — it never participates in an
/// authorization decision — and exists so the UI can tell a user who to talk to about their
/// access instead of leaving them at a dead end. Optional with a default so existing
/// construction sites (including tests) keep compiling unchanged.
/// </param>
public sealed record PermissionResolution(
    IReadOnlyCollection<string> RoleNames,
    bool IsSuperAdmin,
    IReadOnlyCollection<string> Permissions,
    string? AccessGrantedBy = null);
