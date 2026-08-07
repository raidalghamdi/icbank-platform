using System.Security.Claims;

namespace Icbank.Platform.Api.Auth;

/// <summary>Reads the authenticated caller's numeric user id from the access token's subject claim.</summary>
public static class CurrentUserId
{
    /// <summary>Attempts to read the numeric user id from the given principal.</summary>
    /// <param name="user">The current request's <see cref="ClaimsPrincipal"/>.</param>
    /// <returns>The user's id, or <c>null</c> if unauthenticated or the claim is missing/malformed.</returns>
    public static int? TryRead(ClaimsPrincipal user)
    {
        var claimValue = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return int.TryParse(claimValue, out var userId) ? userId : null;
    }
}
