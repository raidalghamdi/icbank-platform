namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>Result of issuing a new access token: the encoded JWT plus its absolute expiry.</summary>
/// <param name="AccessToken">The signed, encoded JWT.</param>
/// <param name="ExpiresAtUtc">The UTC instant the token stops being valid.</param>
public sealed record AccessTokenResult(string AccessToken, DateTime ExpiresAtUtc);
