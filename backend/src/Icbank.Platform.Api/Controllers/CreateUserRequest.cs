namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>POST /api/v1/admin/users</c>.</summary>
/// <param name="Email">The new user's email address.</param>
/// <param name="Name">The new user's display name.</param>
/// <param name="Title">The new user's job title, if any.</param>
/// <param name="Department">The new user's department, if any.</param>
/// <param name="RoleId">The role to assign at creation time.</param>
/// <param name="Password">An optional caller-supplied initial password.</param>
public sealed record CreateUserRequest(string Email, string Name, string? Title, string? Department, int RoleId, string? Password);
