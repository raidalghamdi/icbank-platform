using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Auth;
using Icbank.Platform.Application.Auth.Commands;
using Icbank.Platform.Application.Auth.Queries;
using Icbank.Platform.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Password authentication endpoints (API-SURFACE.md §2). Every endpoint here composes with the
/// httpOnly refresh-token cookie contract in <see cref="RefreshTokenCookieWriter"/> — no endpoint
/// in this controller ever returns the raw refresh token in a JSON body.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/auth")]
public sealed class AuthController : ControllerBase
{
    private const int RefreshCookieFallbackHours = 8;

    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="AuthController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch auth commands/queries.</param>
    public AuthController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Authenticates a user by email and password (BUSINESS-RULES.md §10.5 lockout applies).</summary>
    /// <param name="request">The login credentials.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the access token and user profile; sets the httpOnly refresh-token cookie.</returns>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<ActionResult<AuthenticatedUserDto>> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        Result<LoginResultDto> result = await _sender.Send(new LoginCommand(request.Email, request.Password, ipAddress), cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result.Error, statusCode: MapLoginFailureStatusCode(result.Error));
        }

        LoginResultDto login = result.Value!;
        RefreshTokenCookieWriter.Set(Response, login.RawRefreshToken, DateTime.UtcNow.AddHours(RefreshCookieFallbackHours));
        return Ok(new { accessToken = login.AccessToken, accessTokenExpiresAtUtc = login.AccessTokenExpiresAtUtc, user = login.User });
    }

    /// <summary>Logs out the current session, revoking every active refresh token for the user.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK; clears the refresh-token cookie.</returns>
    [HttpPost("logout")]
    public async Task<ActionResult> LogoutAsync(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId.TryRead(User);
        await _sender.Send(new LogoutCommand(userId), cancellationToken);
        RefreshTokenCookieWriter.Clear(Response);
        return Ok(new { success = true });
    }

    /// <summary>Issues a new access token from the httpOnly refresh-token cookie, rotating the refresh token.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with a new access token; sets a rotated httpOnly refresh-token cookie.</returns>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<ActionResult<AuthenticatedUserDto>> RefreshAsync(CancellationToken cancellationToken)
    {
        if (!Request.Cookies.TryGetValue(CookieAuthConstants.RefreshTokenCookieName, out var rawToken) || string.IsNullOrEmpty(rawToken))
        {
            return Problem("missing_refresh_token", statusCode: StatusCodes.Status401Unauthorized);
        }

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        Result<LoginResultDto> result = await _sender.Send(new RefreshTokenCommand(rawToken, ipAddress), cancellationToken);

        if (!result.IsSuccess)
        {
            RefreshTokenCookieWriter.Clear(Response);
            return Problem(result.Error, statusCode: StatusCodes.Status401Unauthorized);
        }

        LoginResultDto login = result.Value!;
        RefreshTokenCookieWriter.Set(Response, login.RawRefreshToken, DateTime.UtcNow.AddHours(RefreshCookieFallbackHours));
        return Ok(new { accessToken = login.AccessToken, accessTokenExpiresAtUtc = login.AccessTokenExpiresAtUtc, user = login.User });
    }

    /// <summary>Returns the authenticated caller's profile and effective permissions.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the caller's profile.</returns>
    [HttpGet("me")]
    public async Task<ActionResult<AuthenticatedUserDto>> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var userId = CurrentUserId.TryRead(User);
        if (userId is null)
        {
            return Problem("unauthenticated", statusCode: StatusCodes.Status401Unauthorized);
        }

        Result<AuthenticatedUserDto> result = await _sender.Send(new GetCurrentUserQuery(userId.Value), cancellationToken);
        return result.IsSuccess
            ? Ok(result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status404NotFound);
    }

    private static int MapLoginFailureStatusCode(string? error) => error switch
    {
        "account_locked" => StatusCodes.Status403Forbidden,
        "account_inactive" => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status401Unauthorized,
    };
}
