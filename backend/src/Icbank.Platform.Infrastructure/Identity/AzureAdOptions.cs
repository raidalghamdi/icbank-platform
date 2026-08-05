namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>Strongly-typed binding of the <c>AzureAd</c> configuration section.</summary>
public sealed class AzureAdOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "AzureAd";

    /// <summary>Gets or sets a value indicating whether Azure AD SSO is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>Gets or sets the Azure AD tenant id.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the registered application (client) id.</summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>Gets or sets the client secret. Must be supplied via configuration/secret store, never checked in.</summary>
    public string ClientSecret { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional email-domain restriction (BUSINESS-RULES.md §11.3).</summary>
    public string? AllowedDomain { get; set; }

    /// <summary>Gets or sets the server-side redirect URI Azure AD calls back to.</summary>
    public string RedirectUri { get; set; } = string.Empty;

    /// <summary>Gets or sets the allow-list of same-origin relative post-login redirect targets (closes SEC-11).</summary>
    public string[] AllowedRedirectTargets { get; set; } = Array.Empty<string>();
}
