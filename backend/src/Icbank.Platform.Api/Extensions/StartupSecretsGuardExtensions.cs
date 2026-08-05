using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Fails application startup immediately, with a clear message naming every missing key, when a
/// secret that must come from Key Vault in a deployed environment is empty. Prevents the app from
/// ever booting with an empty JWT signing key, cron API key, or database connection string
/// (R-BE-043) -- a silent empty-secret boot is worse than a crash, because it either throws a
/// confusing exception far away from the real cause the first time the value is used, or -- in
/// the JWT case -- would sign tokens with an effectively guessable key.
/// </summary>
public static class StartupSecretsGuardExtensions
{
    private static readonly string[] RequiredKeys =
    {
        "ConnectionStrings:Default",
        "Jwt:SigningKey",
        "Cron:ApiKey",
    };

    /// <summary>
    /// Validates that every required secret is present and non-blank. Skipped in
    /// <c>Development</c> and <c>Testing</c>, where <c>appsettings.Development.json</c> and each
    /// test host's in-memory configuration supply well-known non-production placeholder values
    /// instead of Key Vault.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when one or more required keys are missing or blank, naming every offending key.
    /// </exception>
    public static WebApplicationBuilder AddIcbankStartupSecretsGuard(this WebApplicationBuilder builder)
    {
        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
        {
            return builder;
        }

        var missingKeys = RequiredKeys
            .Where(key => string.IsNullOrWhiteSpace(builder.Configuration[key]))
            .ToArray();

        if (missingKeys.Length > 0)
        {
            throw new InvalidOperationException(
                $"Startup aborted: the following required configuration keys are missing or " +
                $"blank: {string.Join(", ", missingKeys)}. In a deployed environment these must " +
                $"resolve from Key Vault (see KeyVault:VaultUri and docs/DEPLOYMENT.md) -- the " +
                $"app will not boot with an empty connection string, JWT signing key, or cron API " +
                $"key.");
        }

        return builder;
    }
}
