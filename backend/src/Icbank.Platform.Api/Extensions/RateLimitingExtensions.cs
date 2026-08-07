using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.DependencyInjection;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Registers the in-box ASP.NET Core rate limiter (R-BE-073), partitioned by caller identity so
/// one abusive client cannot exhaust another's quota.
/// </summary>
public static class RateLimitingExtensions
{
    private const int AuthPermitLimit = 20;
    private const int ApiPermitLimit = 300;
    private const int ApiWindowSegments = 4;
    private static readonly TimeSpan OneMinuteWindow = TimeSpan.FromMinutes(1);

    /// <summary>Adds the "auth" and "api" rate-limiter policies.</summary>
    /// <param name="services">The DI service collection.</param>
    /// <returns>The same <paramref name="services"/> instance, for chaining.</returns>
    public static IServiceCollection AddIcbankRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = AuthPermitLimit,
                    Window = OneMinuteWindow,
                }));

            options.AddPolicy("api", context => RateLimitPartition.GetSlidingWindowLimiter(
                partitionKey: context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                factory: _ => new SlidingWindowRateLimiterOptions
                {
                    PermitLimit = ApiPermitLimit,
                    Window = OneMinuteWindow,
                    SegmentsPerWindow = ApiWindowSegments,
                }));
        });

        return services;
    }
}
