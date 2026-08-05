using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Exceptions;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Configures Serilog structured logging (R-BE-050, R-BE-052) driven entirely by
/// <c>appsettings.json</c>, so log levels and sinks differ per environment without a rebuild.
/// </summary>
public static class SerilogExtensions
{
    /// <summary>Wires Serilog as the host's logging provider, enriched with exception details and log context.</summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    public static WebApplicationBuilder AddIcbankSerilog(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithExceptionDetails());

        return builder;
    }
}
