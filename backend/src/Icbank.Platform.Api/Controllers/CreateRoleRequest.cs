namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/admin/roles</c>.</summary>
/// <param name="Name">The role's unique machine name.</param>
/// <param name="NameAr">The role's Arabic display label.</param>
/// <param name="Description">An optional description.</param>
public sealed record CreateRoleRequest(string Name, string NameAr, string? Description);
