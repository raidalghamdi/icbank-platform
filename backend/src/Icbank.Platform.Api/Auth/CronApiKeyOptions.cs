namespace Icbank.Platform.Api.Auth;

/// <summary>
/// Strongly-typed binding of the <c>Cron</c> configuration section (closes SEC-13: the old
/// system's hardcoded fallback cron secret in source is replaced with a configuration-bound key
/// that is required to be present — no literal fallback exists anywhere in this codebase).
/// </summary>
public sealed class CronApiKeyOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "Cron";

    /// <summary>Gets or sets the shared API key cron/service callers must present. Must be supplied via configuration/secret store.</summary>
    public string ApiKey { get; set; } = string.Empty;
}
