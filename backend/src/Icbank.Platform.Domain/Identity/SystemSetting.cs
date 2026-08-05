using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Identity;

/// <summary>
/// Key/value store for password policy, session duration, and Azure AD configuration
/// (DATA-MODEL.md section 3.1 <c>system_settings</c>). Note: the source system stores
/// <c>azure_ad_client_secret</c> in plaintext in this table; the .NET port should move secrets
/// out to a secrets manager rather than perpetuating that pattern -- see DOMAIN-PORT-NOTES.md.
/// </summary>
public sealed class SystemSetting : AuditableEntity
{
    /// <summary>Gets or sets the unique setting key, e.g. <c>session_duration_minutes</c>.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the setting value.</summary>
    public string Value { get; set; } = string.Empty;
}
