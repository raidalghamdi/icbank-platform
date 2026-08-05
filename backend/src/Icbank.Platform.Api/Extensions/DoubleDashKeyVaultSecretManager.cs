using Azure.Extensions.AspNetCore.Configuration.Secrets;
using Azure.Security.KeyVault.Secrets;

namespace Icbank.Platform.Api.Extensions;

/// <summary>
/// Maps Key Vault secret names using <c>--</c> as the configuration-section separator (Key Vault
/// secret names allow only alphanumerics and hyphens, so <c>ConnectionStrings--Default</c> in the
/// vault becomes <c>ConnectionStrings:Default</c> in <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>),
/// matching the same <c>__</c>-as-<c>:</c> convention already used for environment variables and
/// App Service application settings (see <c>infra/modules/app-service.bicep</c>).
/// </summary>
public sealed class DoubleDashKeyVaultSecretManager : KeyVaultSecretManager
{
    /// <inheritdoc />
    public override string GetKey(KeyVaultSecret secret) => secret.Name.Replace("--", ":", StringComparison.Ordinal);
}
