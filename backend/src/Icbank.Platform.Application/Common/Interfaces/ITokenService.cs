using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port for issuing short-lived JWT access tokens carrying the caller's effective permission set
/// as claims (DOTNET-CONVENTIONS.md §5.1/§5.4). Refresh-token issuance is handled separately by
/// <see cref="IRefreshTokenService"/> because refresh tokens are opaque, hashed, and DB-backed —
/// not JWTs.
/// </summary>
public interface ITokenService
{
    /// <summary>Issues a new access token for the given user and effective permission set.</summary>
    /// <param name="user">The authenticated user.</param>
    /// <param name="roleNames">The union of role machine-names the user currently holds.</param>
    /// <param name="permissions">The effective <c>{pageSlug}:{verb}</c> permission strings, after role-union and override resolution.</param>
    /// <param name="isSuperAdmin">Whether the user holds the distinct super-admin capability (closes SEC-01 — set only from role-union, never inferred from the plain <c>admin</c> role).</param>
    /// <returns>The signed access token and its expiry.</returns>
    AccessTokenResult IssueAccessToken(User user, IReadOnlyCollection<string> roleNames, IReadOnlyCollection<string> permissions, bool isSuperAdmin);
}
