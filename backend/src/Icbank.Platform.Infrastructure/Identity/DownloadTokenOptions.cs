namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Strongly-typed binding of the <c>DownloadTokens</c> configuration section (GAP 2). The
/// <see cref="SigningKey"/> is HMAC'd into the raw token before it is handed to the client, so a
/// token cannot be forged even by someone who can read the database (they'd see only the SHA-256
/// hash of the HMAC output, never the signing key or the raw value) -- mirrors
/// <see cref="JwtOptions.SigningKey"/>'s "configuration/secret store only, never a literal" rule.
/// </summary>
public sealed class DownloadTokenOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "DownloadTokens";

    /// <summary>Gets or sets the HMAC signing key. Must be supplied via configuration/secret store, never checked in.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the token lifetime in seconds. Deliberately short (default 120s) -- long enough for a browser to start the navigation, no longer.</summary>
    public int LifetimeSeconds { get; set; } = 120;
}
