namespace Icbank.Platform.Application.Admin;

/// <summary>Admin-facing single-user detail (API-SURFACE.md §5 <c>GET /admin/users/:id</c>).</summary>
/// <param name="Id">The user's id.</param>
/// <param name="Email">The user's email.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="Title">The user's job title, if known.</param>
/// <param name="Department">The user's department, if known.</param>
/// <param name="RoleNames">The union of role machine-names the user holds.</param>
/// <param name="IsActive">Whether the account is active.</param>
/// <param name="IsLocked">Whether the account is locked out.</param>
/// <param name="MustChangePassword">Whether the user must change their password on next login.</param>
/// <param name="LastLogin">The UTC timestamp of the last successful login, if any.</param>
/// <param name="CreatedAt">The UTC timestamp the account was created.</param>
public sealed record UserDetailDto(
    int Id,
    string Email,
    string Name,
    string? Title,
    string? Department,
    IReadOnlyCollection<string> RoleNames,
    bool IsActive,
    bool IsLocked,
    bool MustChangePassword,
    DateTime? LastLogin,
    DateTime CreatedAt);
