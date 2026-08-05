namespace Icbank.Platform.Application.Admin;

/// <summary>A single role's page × permission grant matrix (API-SURFACE.md §5 <c>GET /admin/roles/:id/permissions</c>).</summary>
/// <param name="Pages">Every seeded page slug, in seed order.</param>
/// <param name="Permissions">Every seeded permission verb name.</param>
/// <param name="Grants">Page slug → the verb names this role currently grants for that page.</param>
public sealed record RolePermissionMatrixDto(
    IReadOnlyList<string> Pages,
    IReadOnlyList<string> Permissions,
    IReadOnlyDictionary<string, IReadOnlyCollection<string>> Grants);
