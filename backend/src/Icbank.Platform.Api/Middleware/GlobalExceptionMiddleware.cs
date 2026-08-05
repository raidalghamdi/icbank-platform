using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Middleware;

/// <summary>
/// Catches every unhandled exception, logs it with full detail, and renders a Problem Details
/// response (R-BE-051, R-BE-079). No empty <c>catch</c> block is ever acceptable; this is the one
/// place in the pipeline allowed to catch <see cref="Exception"/> broadly, and it always logs.
/// </summary>
/// <remarks>
/// Why: CA1031 ("catch a more specific exception") is suppressed at the class level. This
/// middleware is the single, intentional last-resort catch-all mandated by R-BE-051 — every other
/// layer of the app is expected to let exceptions propagate here rather than swallow them locally.
/// CA1848 (LoggerMessage delegates) is suppressed on the log call itself: this exception path is
/// not hot enough to justify the source-generator boilerplate, and the conventions doc's canonical
/// snippet (§3.2) calls <c>ILogger.LogError</c> directly.
/// </remarks>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Design",
    "CA1031:Do not catch general exception types",
    Justification = "This is the mandated single last-resort exception boundary (conventions doc §3.2, R-BE-051).")]
public sealed class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    /// <summary>Initializes a new instance of the <see cref="GlobalExceptionMiddleware"/> class.</summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger used to record full exception detail server-side.</param>
    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    /// <summary>Invokes the next middleware, translating any thrown exception into Problem Details.</summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception exception)
        {
            // Why: R-BE-051 forbids empty catch blocks; every exception is logged with full trace.
            // CA1848 (LoggerMessage delegates) is intentionally not applied here — see class-level remarks.
#pragma warning disable CA1848
            _logger.LogError(exception, "Unhandled exception on {Path}", context.Request.Path);
#pragma warning restore CA1848

            ProblemDetails problem = MapToProblemDetails(exception);
            problem.Extensions["traceId"] = context.TraceIdentifier;

            context.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }

    /// <summary>Maps a thrown exception to the corresponding Problem Details shape (R-BE-031).</summary>
    /// <param name="exception">The exception that was caught.</param>
    /// <returns>A <see cref="ProblemDetails"/> instance with status and title set, never exposing internals.</returns>
    private static ProblemDetails MapToProblemDetails(Exception exception) => exception switch
    {
        ValidationException => new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
        },
        UnauthorizedAccessException => new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
        },
        Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException => new ProblemDetails
        {
            Status = StatusCodes.Status409Conflict,
            Title = "The resource was modified by another request.",
        },
        _ => new ProblemDetails
        {
            // Why: R-BE-079 — never expose ex.Message/StackTrace to the client in production.
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
        },
    };
}
