using Icbank.Platform.Application.Shorfah;
using Microsoft.Extensions.Configuration;

namespace Icbank.Platform.Infrastructure.Shorfah;

/// <summary>
/// Configuration-backed <see cref="IShorfahUrlProvider"/>. Reads <c>Shorfah:FrontendBaseUrl</c>
/// and <c>Shorfah:ApiBaseUrl</c>, falling back to safe local-loopback defaults rather than the
/// Node source's hardcoded production hostnames (BUSINESS-RULES.md §1.7) -- an operator deploying
/// this port must set both values explicitly for links to resolve to the real, current
/// environment.
/// </summary>
public sealed class ConfigurationShorfahUrlProvider : IShorfahUrlProvider
{
    private const string DefaultFrontendBaseUrl = "http://localhost:3000";
    private const string DefaultApiBaseUrl = "http://localhost:5000";

    /// <summary>Initializes a new instance of the <see cref="ConfigurationShorfahUrlProvider"/> class.</summary>
    /// <param name="configuration">The application configuration.</param>
    public ConfigurationShorfahUrlProvider(IConfiguration configuration)
    {
        FrontendBaseUrl = configuration["Shorfah:FrontendBaseUrl"] ?? DefaultFrontendBaseUrl;
        ApiBaseUrl = configuration["Shorfah:ApiBaseUrl"] ?? DefaultApiBaseUrl;
    }

    /// <inheritdoc />
    public string FrontendBaseUrl { get; }

    /// <inheritdoc />
    public string ApiBaseUrl { get; }
}
