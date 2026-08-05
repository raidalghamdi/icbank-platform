using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Icbank.Platform.Api.HealthChecks;

/// <summary>
/// Placeholder readiness check for the caching dependency. Always healthy today because no cache
/// is wired up yet; kept as a named check so <c>/health/ready</c> has a stable shape once a real
/// cache (Redis/distributed cache) is introduced (R-BE-053).
/// </summary>
public sealed class CacheHealthCheck : IHealthCheck
{
    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(HealthCheckResult.Healthy("No external cache configured; check is a no-op placeholder."));
}
