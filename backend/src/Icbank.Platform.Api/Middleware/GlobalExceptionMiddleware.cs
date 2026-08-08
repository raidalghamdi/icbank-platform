using FluentValidation;
using Icbank.Platform.Infrastructure.Gemini;
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

            // Why: WriteAsJsonAsync(object) ignores a Response.ContentType set beforehand and
            // always emits "application/json" unless a content type is passed explicitly here.
            // The line this replaces silently had no effect -- the wire response was never
            // actually application/problem+json despite the assignment above it looking correct.
            await context.Response.WriteAsJsonAsync<ProblemDetails>(problem, options: null, contentType: "application/problem+json");
        }
    }

    /// <summary>
    /// Builds the Problem Details body for a failed FluentValidation run, carrying the per-field
    /// messages the validators authored.
    /// </summary>
    /// <param name="exception">The validation exception whose failures should be surfaced.</param>
    /// <returns>A 400 <see cref="ProblemDetails"/> with <c>detail</c> and an <c>errors</c> dictionary.</returns>
    /// <remarks>
    /// Why: this previously returned only <c>Title = "Validation failed"</c> and dropped every
    /// <see cref="FluentValidation.Results.ValidationFailure"/>. Clients had no way to learn which
    /// field was rejected, so the UI could only show a bare untranslated "Validation failed" with an
    /// empty reason. Validator messages are author-written constants (many already Arabic), not
    /// runtime internals, so surfacing them does not violate R-BE-079's ban on leaking
    /// <c>ex.Message</c>/<c>StackTrace</c> from unexpected exceptions.
    /// </remarks>
    private static ProblemDetails BuildValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Validation failed",
            Detail = string.Join(" ", exception.Errors.Select(failure => failure.ErrorMessage).Distinct(StringComparer.Ordinal)),
        };

        if (errors.Count > 0)
        {
            problem.Extensions["errors"] = errors;
        }

        return problem;
    }

    /// <summary>Maps a thrown exception to the corresponding Problem Details shape (R-BE-031).</summary>
    /// <param name="exception">The exception that was caught.</param>
    /// <returns>A <see cref="ProblemDetails"/> instance with status and title set, never exposing internals.</returns>
    private static ProblemDetails MapToProblemDetails(Exception exception) => exception switch
    {
        ValidationException validation => BuildValidationProblem(validation),

        // Why: the model chain being exhausted is a temporary upstream condition, not a defect in
        // this service. Its message is an author-written Arabic string, so surfacing it does not
        // leak internals under R-BE-079, and staff get "try again in two minutes" instead of a
        // blank "An unexpected error occurred".
        GeminiUnavailableException => new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = GeminiUnavailableException.FallbackMessageAr,
            Detail = GeminiUnavailableException.FallbackMessageAr,
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
