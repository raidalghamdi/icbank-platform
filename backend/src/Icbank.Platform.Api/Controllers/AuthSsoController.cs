using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Auth;
using Icbank.Platform.Application.Auth.Commands;
using Icbank.Platform.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Azure AD SSO endpoints (API-SURFACE.md §3). The authorization-code exchange is entirely
/// server-side end to end — the callback never renders HTML/JS containing a token (closes
/// SEC-04/SEC-05) and the post-login redirect target is validated against a configured allow-list
/// before ever being used (closes SEC-11).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth/sso")]
public sealed class AuthSsoController : ControllerBase
{
    private const int RefreshCookieFallbackHours = 8;

    private readonly ISender _sender;
    private readonly ISsoOptionsProvider _ssoOptions;

    /// <summary>Initializes a new instance of the <see cref="AuthSsoController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch SSO commands.</param>
    /// <param name="ssoOptions">The Azure AD SSO configuration.</param>
    public AuthSsoController(ISender sender, ISsoOptionsProvider ssoOptions)
    {
        _sender = sender;
        _ssoOptions = ssoOptions;
    }

    /// <summary>Reports whether Azure AD SSO is enabled and its domain restriction, if any.</summary>
    /// <returns>200 OK with the SSO configuration summary.</returns>
    [HttpGet("config")]
    [AllowAnonymous]
    public ActionResult GetConfig() => Ok(new { enabled = _ssoOptions.Enabled, domain = _ssoOptions.AllowedDomain });

    /// <summary>Begins the server-side PKCE flow and redirects the browser to Microsoft's login page.</summary>
    /// <param name="redirect">The caller-requested post-login redirect target (validated against the allow-list; closes SEC-11).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>302 redirect to Azure AD, or a Problem response if SSO is disabled.</returns>
    [HttpGet("azure/start")]
    [AllowAnonymous]
    public async Task<ActionResult> StartAsync([FromQuery] string? redirect, CancellationToken cancellationToken)
    {
        Result<string> result = await _sender.Send(new StartSsoLoginCommand(redirect), cancellationToken);
        return result.IsSuccess
            ? Redirect(result.Value!)
            : Problem(result.Error, statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    /// <summary>Completes the PKCE flow: exchanges the code server-side, issues the httpOnly session cookie, and redirects to the validated target.</summary>
    /// <param name="code">The authorization code returned by Azure AD.</param>
    /// <param name="state">The opaque state value, matched against the server-side PKCE store.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>302 redirect to the validated post-login target; sets the httpOnly refresh-token cookie. Never renders a token in HTML/JS.</returns>
    [HttpGet("azure/callback")]
    [AllowAnonymous]
    public async Task<ActionResult> CallbackAsync([FromQuery] string code, [FromQuery] string state, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        Result<SsoCallbackResultDto> result = await _sender.Send(new SsoCallbackCommand(code, state, ipAddress), cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result.Error, statusCode: StatusCodes.Status401Unauthorized);
        }

        SsoCallbackResultDto callback = result.Value!;
        RefreshTokenCookieWriter.Set(Response, callback.Login.RawRefreshToken, DateTime.UtcNow.AddHours(RefreshCookieFallbackHours));

        // Why: closes SEC-04/SEC-05 — the access token is never embedded in this redirect, in
        // HTML, or in inline JS. The SPA retrieves it by calling GET /auth/me (or the frontend's
        // own bootstrap call) once it lands on RedirectTarget, authenticated via the httpOnly
        // refresh cookie just set above.
        return Redirect(callback.RedirectTarget);
    }
}
