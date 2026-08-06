namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// Strongly-typed binding of the <c>Seed</c> configuration section (task requirement 6: the
/// seeder "refuses to run in Production unless an explicit flag is set", and reads the initial
/// super-admin password from configuration/secret rather than generating it unconditionally, so
/// operators can inject a Key-Vault-backed value if they prefer not to rely on the random
/// generator's one-time console output).
/// </summary>
public sealed class SeedOptions
{
    /// <summary>Gets or sets the configuration section name.</summary>
    public const string SectionName = "Seed";

    /// <summary>Gets or sets a value indicating whether seeding is explicitly permitted in the Production environment.</summary>
    public bool AllowInProduction { get; set; }

    /// <summary>Gets or sets the email of the initial super-admin account.</summary>
    public string InitialSuperAdminEmail { get; set; } = "ccteam234@gmail.com";

    /// <summary>Gets or sets an operator-supplied initial super-admin password. If empty, a random password is generated instead.</summary>
    public string InitialSuperAdminPassword { get; set; } = string.Empty;
}
