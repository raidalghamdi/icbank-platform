using Microsoft.AspNetCore.Http;

namespace Icbank.Platform.Api.Middleware;

/// <summary>
/// Adds the standard hardening headers to every response (R-BE-074): clickjacking, MIME-sniffing,
/// referrer-leakage, and camera/microphone/geolocation protections, plus stripping the
/// framework-revealing <c>Server</c> header (R-BE-079).
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private const string DefaultContentSecurityPolicy =
        "default-src 'self'; script-src 'self'; style-src 'self'; img-src 'self' data:; frame-ancestors 'none'";

    private readonly RequestDelegate _next;

    /// <summary>Initializes a new instance of the <see cref="SecurityHeadersMiddleware"/> class.</summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    /// <summary>Sets the mandated security headers before invoking the rest of the pipeline.</summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers["X-Content-Type-Options"] = "nosniff";
        context.Response.Headers["X-Frame-Options"] = "DENY";
        context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        context.Response.Headers["Content-Security-Policy"] = DefaultContentSecurityPolicy;
        context.Response.Headers["Permissions-Policy"] = "geolocation=(), camera=(), microphone=()";
        context.Response.Headers.Remove("Server");

        await _next(context);
    }
}
