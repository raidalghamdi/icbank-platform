namespace Icbank.Platform.Infrastructure.Identity;

/// <summary>
/// Strongly-typed binding of the <c>Jwt</c> configuration section. All values are read from
/// configuration/secrets (R-BE-043) — the signing key must never be a literal in source.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "Jwt";

    /// <summary>Gets or sets the symmetric signing key. Must be supplied via configuration/secret store, never checked in.</summary>
    public string SigningKey { get; set; } = string.Empty;

    /// <summary>Gets or sets the token issuer.</summary>
    public string Issuer { get; set; } = "icbank-platform";

    /// <summary>Gets or sets the token audience.</summary>
    public string Audience { get; set; } = "icbank-platform-clients";

    /// <summary>Gets or sets the access-token lifetime in minutes. DOTNET-CONVENTIONS.md §5.1 mandates ≤ 15 minutes.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Gets or sets the refresh-token lifetime in hours. DOTNET-CONVENTIONS.md §5.1 mandates ≤ 8 hours.</summary>
    public int RefreshTokenHours { get; set; } = 8;
}
