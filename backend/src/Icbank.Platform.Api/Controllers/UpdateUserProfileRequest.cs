namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Request body for <c>PATCH /api/v1/admin/users/{userId}</c>. Deliberately has no <c>roleId</c>
/// field — role changes go through the dedicated super-admin-only
/// <c>POST /api/v1/admin/users/{userId}/roles</c> endpoint (closes SEC-01).
/// </summary>
/// <param name="Name">The new display name, if changing.</param>
/// <param name="Title">The new job title, if changing.</param>
/// <param name="Department">The new department, if changing.</param>
/// <param name="Email">The new email address, if changing.</param>
public sealed record UpdateUserProfileRequest(string? Name, string? Title, string? Department, string? Email);
