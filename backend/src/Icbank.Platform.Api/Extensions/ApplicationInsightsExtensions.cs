using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Wires the Application Insights ASP.NET Core SDK for request/dependency/exception
/// auto-collection, and to provide the <c>TelemetryConfiguration</c> the Serilog
/// <c>Serilog.Sinks.ApplicationInsights</c> sink should share rather than build its own (the
/// package's own guidance -- a second, independently constructed <c>TelemetryConfiguration</c>
/// would not correlate request telemetry with log telemetry). Deliberately opt-in via
/// <c>ApplicationInsights:ConnectionString</c> so local development and the test suite, which
/// never set that key, incur zero telemetry SDK overhead.
/// </summary>
public static class ApplicationInsightsExtensions
{
    /// <summary>Registers the Application Insights SDK when a connection string is configured.</summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    public static WebApplicationBuilder AddIcbankApplicationInsights(this WebApplicationBuilder builder)
    {
        var connectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            return builder;
        }

        builder.Services.AddApplicationInsightsTelemetry(options => options.ConnectionString = connectionString);

        return builder;
    }
}
