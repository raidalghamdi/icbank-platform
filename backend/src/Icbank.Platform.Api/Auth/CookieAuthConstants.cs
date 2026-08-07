namespace Icbank.Platform.Api.Auth;

/// <summary>Names and options shared by every endpoint that sets or reads the refresh-token cookie.</summary>
public static class CookieAuthConstants
{
    /// <summary>The refresh-token cookie name.</summary>
    public const string RefreshTokenCookieName = "refresh_token";

    /// <summary>The path the refresh-token cookie is scoped to — only the auth endpoints ever need to read it.</summary>
    public const string RefreshTokenCookiePath = "/api/v1/auth";
}
