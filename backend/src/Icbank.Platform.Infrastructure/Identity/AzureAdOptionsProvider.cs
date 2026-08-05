using Icbank.Platform.Application.Auth;
using Microsoft.Extensions.Options;

namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>Adapts <see cref="AzureAdOptions"/> to the Application-layer <see cref="ISsoOptionsProvider"/> port.</summary>
public sealed class AzureAdOptionsProvider : ISsoOptionsProvider
{
    private readonly AzureAdOptions _options;

    /// <summary>Initializes a new instance of the <see cref="AzureAdOptionsProvider"/> class.</summary>
    /// <param name="options">The bound Azure AD configuration options.</param>
    public AzureAdOptionsProvider(IOptions<AzureAdOptions> options)
    {
        _options = options.Value;
    }

    /// <inheritdoc />
    public bool Enabled => _options.Enabled;

    /// <inheritdoc />
    public string? AllowedDomain => _options.AllowedDomain;

    /// <inheritdoc />
    public IReadOnlyCollection<string> AllowedRedirectTargets => _options.AllowedRedirectTargets;
}
