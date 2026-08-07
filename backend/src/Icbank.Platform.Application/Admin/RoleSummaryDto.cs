namespace Icbank.Platform.Application.Admin;

/// <summary>Admin-facing role summary row (API-SURFACE.md §5 <c>GET /admin/roles</c>).</summary>
/// <param name="Id">The role's id.</param>
/// <param name="Name">The role's machine name.</param>
/// <param name="NameAr">The role's Arabic display label.</param>
/// <param name="Description">An optional description.</param>
/// <param name="IsSystem">Whether this is a system role that cannot be deleted.</param>
/// <param name="UserCount">The number of users currently assigned this role.</param>
public sealed record RoleSummaryDto(int Id, string Name, string NameAr, string? Description, bool IsSystem, int UserCount);
