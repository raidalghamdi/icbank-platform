namespace Icbank.Platform.Application.Auth;

/// <summary>Result of a successful login/refresh call. The raw refresh token is set as an httpOnly cookie by the Api layer — it is never returned in the JSON body.</summary>
/// <param name="AccessToken">The short-lived JWT access token.</param>
/// <param name="AccessTokenExpiresAtUtc">The access token's absolute expiry.</param>
/// <param name="RawRefreshToken">The raw refresh-token value, for the Api layer to set as a cookie. Never serialized to JSON.</param>
/// <param name="User">The authenticated user's profile and effective permissions.</param>
public sealed record LoginResultDto(string AccessToken, DateTime AccessTokenExpiresAtUtc, string RawRefreshToken, AuthenticatedUserDto User);
