namespace Icbank.Platform.Application.Auth;

/// <summary>Result of a completed SSO callback, including the already-validated post-login redirect target (closes SEC-11).</summary>
/// <param name="Login">The issued session (access token, refresh token, user profile).</param>
/// <param name="RedirectTarget">The allow-listed post-login redirect target.</param>
public sealed record SsoCallbackResultDto(LoginResultDto Login, string RedirectTarget);
