extern alias identity;

using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;
using Microsoft.AspNetCore.Builder;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Wires Azure Key Vault as a configuration source using the App Service's system-assigned
/// managed identity (R-BE-043: no secret ever lives in <c>appsettings.json</c>, a Bicep file, or
/// a workflow). Deliberately opt-in via <c>KeyVault:VaultUri</c> so local development and the test
/// suite — which never set that key — are completely unaffected and need no cloud dependency.
/// </summary>
public static class KeyVaultExtensions
{
    /// <summary>
    /// Adds the Key Vault secrets configuration provider when <c>KeyVault:VaultUri</c> is present.
    /// <c>Azure.Identity.DefaultAzureCredential</c> resolves to the App Service's managed identity when
    /// deployed, and to a developer's own Azure CLI/VS credential if someone opts in locally by
    /// setting the app setting — it never requires a client secret in configuration.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The same <paramref name="builder"/> instance, for chaining.</returns>
    public static WebApplicationBuilder AddIcbankKeyVault(this WebApplicationBuilder builder)
    {
        var vaultUri = builder.Configuration["KeyVault:VaultUri"];
        if (string.IsNullOrWhiteSpace(vaultUri))
        {
            return builder;
        }

        var client = new SecretClient(new Uri(vaultUri), new identity::Azure.Identity.DefaultAzureCredential());
        builder.Configuration.AddAzureKeyVault(client, new AzureKeyVaultConfigurationOptions
        {
            // Why: secret names cannot contain ':', so Key Vault secrets use '--' as the
            // hierarchy separator (e.g. "ConnectionStrings--Default") and this maps them back
            // onto .NET's standard ':' configuration-key separator on load.
            Manager = new DoubleDashKeyVaultSecretManager(),
        });

        // Why: the vault provider is appended after the host's default environment-variable
        // provider, so a stale vault copy of a secret silently shadows the value the platform is
        // explicitly configured with -- last provider wins. On 2026-08-06 that took the dev API
        // down: the vault still held the pre-rotation SQL password while the correct one sat in
        // App Service configuration, so every start died with SQL error 18456 ("Login failed")
        // during seeding, before a single request was served. Re-adding the environment-variable
        // provider last makes explicitly-set platform configuration authoritative, while the vault
        // still supplies every secret the platform does not set (Jwt:SigningKey, Cron:ApiKey,
        // DownloadTokens:SigningKey). Idempotent: adding the provider twice is safe.
        builder.Configuration.AddEnvironmentVariables();

        return builder;
    }
}
