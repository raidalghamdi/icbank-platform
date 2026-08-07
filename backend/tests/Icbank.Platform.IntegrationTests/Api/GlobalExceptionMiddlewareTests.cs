using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using Icbank.Platform.Api.Middleware;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Api;

/// <summary>
/// Verifies <see cref="GlobalExceptionMiddleware"/> renders the exact Problem Details contract
/// (R-BE-031, R-BE-051, R-BE-079) for every exception shape it special-cases, and -- critically --
/// that it never leaks <c>ex.Message</c>/stack trace for the unmapped, 500-mapped case. This
/// middleware previously had zero direct test coverage even though it is the single last-resort
/// response boundary for the whole Api layer.
/// </summary>
public sealed class GlobalExceptionMiddlewareTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task InvokeAsync_ValidationException_Returns400WithValidationTitle()
    {
        HttpContext context = await InvokeWithThrowingNext(
            () => throw new ValidationException("irrelevant detail"));

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        ProblemDetails problem = await ReadProblemDetails(context);
        problem.Title.Should().Be("Validation failed");
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_SurfacesPerFieldMessagesInDetailAndErrors()
    {
        // Why: the middleware used to return only Title = "Validation failed" and discard every
        // ValidationFailure. Clients had no way to render which field was rejected, so the UI showed
        // a bare untranslated "Validation failed" with an empty reason on every 400.
        FluentValidation.Results.ValidationFailure[] failures = new[]
        {
            new FluentValidation.Results.ValidationFailure("PeriodLabel", "فترة التقرير (periodLabel) مطلوبة."),
            new FluentValidation.Results.ValidationFailure("Size", "المقاس مطلوب (square | story | landscape)"),
        };

        HttpContext context = await InvokeWithThrowingNext(() => throw new ValidationException(failures));

        context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var body = await ReadResponseBody(context);
        body.Should().Contain("errors");

        ProblemDetails problem = JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions)!;
        problem.Detail.Should().Contain("periodLabel").And.Contain("المقاس");

        Dictionary<string, string[]> errors = JsonSerializer.Deserialize<Dictionary<string, string[]>>(
            problem.Extensions["errors"]!.ToString()!, JsonOptions)!;
        errors.Should().ContainKey("PeriodLabel");
        errors.Should().ContainKey("Size");
        errors["PeriodLabel"].Should().ContainSingle().Which.Should().Contain("periodLabel");
    }

    [Fact]
    public async Task InvokeAsync_UnauthorizedAccessException_Returns403WithForbiddenTitle()
    {
        HttpContext context = await InvokeWithThrowingNext(
            () => throw new UnauthorizedAccessException("irrelevant detail"));

        context.Response.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        ProblemDetails problem = await ReadProblemDetails(context);
        problem.Title.Should().Be("Forbidden");
    }

    [Fact]
    public async Task InvokeAsync_DbUpdateConcurrencyException_Returns409WithConflictTitle()
    {
        HttpContext context = await InvokeWithThrowingNext(
            () => throw new DbUpdateConcurrencyException("irrelevant detail"));

        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        ProblemDetails problem = await ReadProblemDetails(context);
        problem.Title.Should().Be("The resource was modified by another request.");
    }

    [Fact]
    public async Task InvokeAsync_UnmappedException_Returns500WithoutLeakingExceptionMessage()
    {
        const string secretDetail = "connection string password=hunter2 stack frame at Internal.Namespace";
        HttpContext context = await InvokeWithThrowingNext(
            () => throw new InvalidOperationException(secretDetail));

        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        context.Response.ContentType.Should().Be("application/problem+json");

        var body = await ReadResponseBody(context);
        body.Should().NotContain(secretDetail, "R-BE-079 forbids leaking exception detail to the client");
        body.Should().NotContain("InvalidOperationException");

        ProblemDetails problem = JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions)!;
        problem.Title.Should().Be("An unexpected error occurred");
    }

    [Fact]
    public async Task InvokeAsync_AnyException_SetsTraceIdExtensionFromHttpContext()
    {
        HttpContext context = await InvokeWithThrowingNext(
            () => throw new InvalidOperationException("boom"),
            configureContext: c => c.TraceIdentifier = "trace-abc-123");

        ProblemDetails problem = await ReadProblemDetails(context);
        problem.Extensions["traceId"]!.ToString().Should().Be("trace-abc-123");
    }

    [Fact]
    public async Task InvokeAsync_NoExceptionThrown_PassesThroughWithoutModifyingResponse()
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => Task.CompletedTask,
            NullLogger<GlobalExceptionMiddleware>.Instance);
        HttpContext context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context);

        context.Response.StatusCode.Should().Be(StatusCodes.Status200OK, "the default, untouched status code");
        context.Response.Body.Length.Should().Be(0);
    }

    private static async Task<HttpContext> InvokeWithThrowingNext(Action throwingAction, Action<HttpContext>? configureContext = null)
    {
        var middleware = new GlobalExceptionMiddleware(
            _ => ThrowAndCompleteSynchronously(throwingAction),
            NullLogger<GlobalExceptionMiddleware>.Instance);
        HttpContext context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        configureContext?.Invoke(context);

        await middleware.InvokeAsync(context);
        return context;
    }

    private static async Task<string> ReadResponseBody(HttpContext context)
    {
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    private static async Task<ProblemDetails> ReadProblemDetails(HttpContext context)
    {
        var body = await ReadResponseBody(context);
        return JsonSerializer.Deserialize<ProblemDetails>(body, JsonOptions)!;
    }

    private static Task ThrowAndCompleteSynchronously(Action throwingAction)
    {
        throwingAction();
        return Task.CompletedTask;
    }
}
