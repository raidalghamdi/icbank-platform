namespace Icbank.Platform.Application.Auth;

/// <summary>Public-facing user profile plus effective permissions, returned by login/refresh/me (API-SURFACE.md §2).</summary>
/// <param name="Id">The user's id.</param>
/// <param name="Email">The user's email address.</param>
/// <param name="Name">The user's display name.</param>
/// <param name="RoleNames">The union of role machine-names the user holds.</param>
/// <param name="IsSuperAdmin">Whether the user holds the distinct super-admin capability.</param>
/// <param name="Permissions">The effective <c>{pageSlug}:{verb}</c> permission strings.</param>
/// <param name="MustChangePassword">Whether the user must change their password before continuing (forced first-login reset).</param>
public sealed record AuthenticatedUserDto(
    int Id,
    string Email,
    string Name,
    IReadOnlyCollection<string> RoleNames,
    bool IsSuperAdmin,
    IReadOnlyCollection<string> Permissions,
    bool MustChangePassword);
