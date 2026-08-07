namespace Icbank.Platform.Application.Admin;

/// <summary>
/// The whitelist of mutable <c>system_settings</c> keys (BUSINESS-RULES.md §10.4/§11.1:
/// password policy, session duration, Azure AD config), mirroring the old system's
/// <c>SETTINGS_SCHEMA</c> allowlist ("Whitelist-validated against SETTINGS_SCHEMA" per
/// API-SURFACE.md §5 <c>PUT /admin/settings</c>). Any key not in this set is rejected outright —
/// this is the only thing standing between an admin request body and arbitrary key/value writes
/// to a table that also holds the Azure AD client secret.
/// </summary>
public static class SystemSettingsSchema
{
    /// <summary>Minimum password length.</summary>
    public const string PasswordMinLength = "password_min_length";

    /// <summary>Whether an uppercase character is required in passwords.</summary>
    public const string PasswordRequireUppercase = "password_require_uppercase";

    /// <summary>Whether a special character is required in passwords.</summary>
    public const string PasswordRequireSpecialChar = "password_require_special_char";

    /// <summary>Password expiry in days; <c>0</c> disables expiry.</summary>
    public const string PasswordExpiryDays = "password_expiry_days";

    /// <summary>Access-token session duration in minutes.</summary>
    public const string SessionDurationMinutes = "session_duration_minutes";

    /// <summary>The Azure AD tenant id.</summary>
    public const string AzureAdTenantId = "azure_ad_tenant_id";

    /// <summary>The Azure AD application (client) id.</summary>
    public const string AzureAdClientId = "azure_ad_client_id";

    /// <summary>The Azure AD client secret. Never returned in plaintext by any read endpoint (closes the old system's plaintext-secret-exposure gap).</summary>
    public const string AzureAdClientSecret = "azure_ad_client_secret";

    /// <summary>The required email domain for SSO logins, if domain-restricted.</summary>
    public const string AzureAdDomain = "azure_ad_domain";

    /// <summary>Gets every whitelisted setting key.</summary>
    public static IReadOnlyCollection<string> AllKeys { get; } = new[]
    {
        PasswordMinLength, PasswordRequireUppercase, PasswordRequireSpecialChar, PasswordExpiryDays,
        SessionDurationMinutes, AzureAdTenantId, AzureAdClientId, AzureAdClientSecret, AzureAdDomain,
    };

    /// <summary>Gets the subset of keys whose values must never be echoed back in plaintext by a read endpoint.</summary>
    public static IReadOnlyCollection<string> SecretKeys { get; } = new[] { AzureAdClientSecret };
}
