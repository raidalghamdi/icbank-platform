namespace Icbank.Platform.Api.Controllers;

/// <summary>The Node-compatible health-check response shape.</summary>
/// <param name="Status">Always <c>"ok"</c> when the process is up.</param>
public sealed record HealthCheckResponse(string Status);
