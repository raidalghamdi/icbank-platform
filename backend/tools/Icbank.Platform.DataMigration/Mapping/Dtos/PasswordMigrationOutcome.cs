namespace Icbank.Platform.DataMigration.Mapping.Dtos;

/// <summary>
/// Records which password-portability decision (task requirement 3) applied to one migrated
/// user, so the final report can enumerate exactly who must reset their password rather than
/// silently forcing it (see docs/DATA-MIGRATION.md "Password and refresh-token portability").
/// </summary>
public enum PasswordMigrationOutcome
{
    /// <summary>The source user had no password hash (SSO-only via Azure AD) — nothing to migrate or reset.</summary>
    SsoOnlyNoPasswordToMigrate = 0,

    /// <summary>
    /// The source user had a bcrypt password hash. Bcrypt and ASP.NET Identity's PBKDF2-based
    /// format are mutually incompatible, so the hash cannot be carried over — the destination
    /// row is written with a null <c>PasswordHash</c> and <c>MustChangePassword = true</c>.
    /// </summary>
    BcryptHashNotPortableMustReset = 1,
}
