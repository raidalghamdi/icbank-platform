using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Middleware;

/// <summary>
/// Stops a temporary-password access token from reaching application data. The restriction is
/// claim-based so it applies consistently before controllers and authorization policies run.
/// Only the profile endpoint and the self-service password-change endpoint remain reachable.
/// </summary>
public sealed class MustChangePasswordMiddleware
{
    private const string MustChangePasswordClaimType = "must_change_password";
    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of the <see cref="MustChangePasswordMiddleware"/> class.</summary>
    /// <param name="next">The next request delegate.</param>
    public MustChangePasswordMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Applies the temporary-password gate to authenticated requests.</summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!RequiresPasswordChange(context.User) || IsAllowedEndpoint(context.Request.Path))
        {
            await _next(context);
            return;
        }

        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        context.Response.ContentType = "application/problem+json";
        await context.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "must_change_password",
            Detail = "Change the temporary password before accessing application data.",
        });
    }

    private static bool RequiresPasswordChange(ClaimsPrincipal principal) =>
        principal.Identity?.IsAuthenticated == true &&
        string.Equals(principal.FindFirstValue(MustChangePasswordClaimType), bool.TrueString, StringComparison.Ordinal);

    private static bool IsAllowedEndpoint(PathString path) =>
        path.Equals("/api/v1/auth/me", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/api/v1/auth/change-password", StringComparison.OrdinalIgnoreCase);
}
