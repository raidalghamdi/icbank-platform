namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/admin/users/{userId}/roles</c>.</summary>
/// <param name="RoleId">The role to assign.</param>
public sealed record AssignRoleRequest(int RoleId);
