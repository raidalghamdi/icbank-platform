using Microsoft.AspNetCore.Authorization;

namespace Icbank.Platform.Api.Auth;

/// <summary>Marker requirement for the cron/service-to-service API key policy.</summary>
public sealed class CronApiKeyRequirement : IAuthorizationRequirement
{
    /// <summary>Gets the singleton instance.</summary>
    public static CronApiKeyRequirement Instance { get; } = new();
}
