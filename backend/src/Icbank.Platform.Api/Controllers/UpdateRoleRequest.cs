namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PATCH /api/v1/admin/roles/{roleId}</c>.</summary>
/// <param name="NameAr">The new Arabic display label, if changing.</param>
/// <param name="Description">The new description, if changing.</param>
public sealed record UpdateRoleRequest(string? NameAr, string? Description);
