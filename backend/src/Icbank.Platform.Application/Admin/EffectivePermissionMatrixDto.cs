using Icbank.Platform.Application.Common.Models;

namespace Icbank.Platform.Application.Admin;

/// <summary>Full effective permission matrix across a page of users (API-SURFACE.md §5 <c>GET /admin/matrix</c>).</summary>
/// <param name="Pages">Every seeded page slug, in seed order.</param>
/// <param name="Permissions">Every seeded permission verb name.</param>
/// <param name="Users">The paginated per-user effective grants (R-BE-033 — the old system returned every user unbounded; this port paginates).</param>
public sealed record EffectivePermissionMatrixDto(
    IReadOnlyList<string> Pages,
    IReadOnlyList<string> Permissions,
    PagedResult<UserEffectivePermissionsDto> Users);
