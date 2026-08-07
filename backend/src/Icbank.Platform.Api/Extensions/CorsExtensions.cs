using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Registers a configuration-driven CORS allow-list (R-BE-032, R-FE-023). Origins are never
/// wildcarded — the old system's <c>origin: true</c> vulnerability this rewrite must not repeat —
/// and are read from <c>Cors:AllowedOrigins</c> so environments can differ without a rebuild.
/// </summary>
public static class CorsExtensions
{
    /// <summary>The name of the single CORS policy the API exposes.</summary>
    public const string FrontendPolicyName = "frontend";

    private static readonly string[] AllowedMethods = { "GET", "POST", "PUT", "PATCH", "DELETE" };

    /// <summary>Adds the "frontend" CORS policy, sourced from <c>Cors:AllowedOrigins</c> configuration.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddIcbankCors(this IServiceCollection services, IConfiguration configuration)
    {
        var allowedOrigins = configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? Array.Empty<string>();

        services.AddCors(options => options.AddPolicy(FrontendPolicyName, BuildPolicy(allowedOrigins)));

        return services;
    }

    /// <summary>Builds the allow-list CORS policy for the given explicit origin set.</summary>
    /// <param name="allowedOrigins">The exact origins permitted to call this API with credentials.</param>
    /// <returns>A delegate configuring a single named <see cref="CorsPolicyBuilder"/>.</returns>
    private static Action<CorsPolicyBuilder> BuildPolicy(string[] allowedOrigins) => policy =>
    {
        // Why: AllowCredentials() requires an explicit origin allow-list — CORS forbids combining
        // it with AllowAnyOrigin(), which is exactly the vulnerability this replaces (R-BE-032).
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .WithMethods(AllowedMethods)
            .AllowCredentials();
    };
}
