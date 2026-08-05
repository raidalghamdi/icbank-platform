using System.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Serilog.Context;

namespace Icbank.Platform.Api.Middleware;

/// <summary>
/// Registers the inline correlation-id middleware (R-BE-052): the W3C trace id that ASP.NET Core
/// populates automatically is pushed into the Serilog log context and mirrored back to the client
/// as <c>X-Correlation-Id</c>, so every log line and every response can be joined to one request.
/// </summary>
public static class CorrelationIdMiddlewareExtensions
{
    private const string CorrelationHeaderName = "X-Correlation-Id";

    /// <summary>Adds the correlation-id propagation middleware to the pipeline.</summary>
    /// <param name="app">The application builder.</param>
    /// <returns>The same <paramref name="app"/> instance, for chaining.</returns>
    public static IApplicationBuilder UseCorrelationId(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            var traceId = Activity.Current?.TraceId.ToString() ?? context.TraceIdentifier;
            using (LogContext.PushProperty("TraceId", traceId))
            {
                context.Response.Headers[CorrelationHeaderName] = traceId;
                await next();
            }
        });
}
