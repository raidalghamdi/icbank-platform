namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>The result of resolving a user's effective permissions.</summary>
/// <param name="RoleNames">The union of role machine-names the user holds (may be more than one — see DOMAIN-PORT-NOTES.md multi-role fix).</param>
/// <param name="IsSuperAdmin">Whether the user holds the distinct super-admin capability (closes SEC-01 — never true for plain <c>admin</c>).</param>
/// <param name="Permissions">The effective <c>{pageSlug}:{verb}</c> permission strings.</param>
public sealed record PermissionResolution(IReadOnlyCollection<string> RoleNames, bool IsSuperAdmin, IReadOnlyCollection<string> Permissions);
