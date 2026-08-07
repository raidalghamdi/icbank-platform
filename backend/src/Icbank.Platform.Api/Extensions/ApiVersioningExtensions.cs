using Asp.Versioning;
using Microsoft.Extensions.DependencyInjection;

namespace Icbank.Platform.Api.Extensions;

/// <summary>Registers URL-segment API versioning (R-BE-030): every route must carry an explicit version.</summary>
public static class ApiVersioningExtensions
{
    /// <summary>Adds API versioning with Swashbuckle-compatible API Explorer grouping.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddIcbankApiVersioning(this IServiceCollection services)
    {
        services.AddApiVersioning(options =>
            {
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = false; // Why: un-versioned routes are forbidden.
                options.ReportApiVersions = true;
            })
            .AddMvc()
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
