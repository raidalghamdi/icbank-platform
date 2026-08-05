using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Application.Auth;

/// <summary>
/// Shared helper that turns a resolved user + permission set into an access token and DTO —
/// used identically by login, refresh, SSO callback, and the current-user endpoint so the four
/// endpoints can never drift in what claims/shape they issue.
/// </summary>
public sealed class AuthSessionFactory
{
    private readonly ITokenService _tokenService;

    /// <summary>Initializes a new instance of the <see cref="AuthSessionFactory"/> class.</summary>
    /// <param name="tokenService">The access-token issuance port.</param>
    public AuthSessionFactory(ITokenService tokenService)
    {
        _tokenService = tokenService;
    }

    /// <summary>Builds the access token and the public-facing <see cref="AuthenticatedUserDto"/> for a resolved user.</summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="resolution">The user's resolved effective permissions.</param>
    /// <param name="mustChangePassword">Whether the user must change their password before continuing.</param>
    /// <returns>The signed access token and the public user profile.</returns>
    public (AccessTokenResult AccessToken, AuthenticatedUserDto User) BuildSession(
        User user, PermissionResolution resolution, bool mustChangePassword)
    {
        AccessTokenResult accessToken = _tokenService.IssueAccessToken(
            user, resolution.RoleNames, resolution.Permissions, resolution.IsSuperAdmin);

        var dto = new AuthenticatedUserDto(
            user.Id,
            user.Email,
            user.Name,
            resolution.RoleNames,
            resolution.IsSuperAdmin,
            resolution.Permissions,
            mustChangePassword);

        return (accessToken, dto);
    }
}
