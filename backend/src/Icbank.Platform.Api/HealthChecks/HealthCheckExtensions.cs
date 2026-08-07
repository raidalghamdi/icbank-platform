using Icbank.Platform.Api.HealthChecks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Registers liveness/readiness health checks and their endpoints (R-BE-053): <c>/health/live</c>
/// answers "is the process up" with no dependency checks, while <c>/health/ready</c> additionally
/// verifies SQL Server and any other downstream dependency is reachable.
/// </summary>
public static class HealthCheckExtensions
{
    private const string ReadyTag = "ready";

    /// <summary>Adds the SQL Server and cache readiness checks.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration, used for the SQL Server connection string.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddIcbankHealthChecks(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

        services.AddHealthChecks()
            .AddSqlServer(connectionString, name: "sql-server", tags: new[] { ReadyTag })
            .AddCheck<CacheHealthCheck>("cache", tags: new[] { ReadyTag });

        return services;
    }

    /// <summary>Maps the <c>/health/live</c> and <c>/health/ready</c> endpoints.</summary>
    /// <param name="app">The endpoint route builder.</param>
    /// <returns>The same <paramref name="app"/> instance, for chaining.</returns>
    public static WebApplication MapIcbankHealthChecks(this WebApplication app)
    {
        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = check => check.Tags.Contains(ReadyTag) });
        return app;
    }
}
