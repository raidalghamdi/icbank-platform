using Microsoft.AspNetCore.Http;

namespace Icbank.Platform.Api.Auth;

/// <summary>
/// Sets/clears the refresh-token cookie with the exact flags DOTNET-CONVENTIONS.md §5.1 mandates
/// (httpOnly + Secure + SameSite=Strict, set server-side only) — this is the single call site for
/// that cookie so every auth endpoint (login, refresh, SSO callback) is guaranteed to agree,
/// closing SEC-04/SEC-05 (the old system wrote a non-httpOnly, client-JS-settable cookie from the
/// SSO callback specifically; here there is exactly one code path and it is always server-side,
/// always httpOnly).
/// </summary>
public static class RefreshTokenCookieWriter
{
    /// <summary>Sets the refresh-token cookie on the response.</summary>
    /// <param name="response">The HTTP response to set the cookie on.</param>
    /// <param name="rawToken">The raw (unhashed) refresh-token value.</param>
    /// <param name="expiresAtUtc">The cookie's absolute expiry, matching the token's own expiry.</param>
    public static void Set(HttpResponse response, string rawToken, DateTime expiresAtUtc)
    {
        response.Cookies.Append(CookieAuthConstants.RefreshTokenCookieName, rawToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookieAuthConstants.RefreshTokenCookiePath,
            Expires = expiresAtUtc,
        });
    }

    /// <summary>Clears the refresh-token cookie on the response (logout).</summary>
    /// <param name="response">The HTTP response to clear the cookie on.</param>
    public static void Clear(HttpResponse response)
    {
        response.Cookies.Delete(CookieAuthConstants.RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Path = CookieAuthConstants.RefreshTokenCookiePath,
        });
    }
}
