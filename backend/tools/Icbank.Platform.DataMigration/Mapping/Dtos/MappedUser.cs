namespace Icbank.Platform.DataMigration.Mapping.Dtos;

/// <summary>
/// Pure DTO shape produced by <see cref="Transformers.UserTransformer"/> from a raw
/// <see cref="Source.SourceRow"/>. Crosses from the Mapping layer into the Migration writer —
/// never an EF entity itself, kept as a plain data carrier so the transformer stays unit
/// testable without EF Core in the loop (task requirement: pure, injectable transformation
/// functions).
/// </summary>
/// <param name="SourceId">The source Postgres <c>users.id</c>.</param>
/// <param name="Email">The user's email (natural key — <c>ux_users_email</c> unique in both schemas).</param>
/// <param name="Name">The display name.</param>
/// <param name="Title">The optional job title.</param>
/// <param name="Department">The optional department.</param>
/// <param name="PasswordHash">
/// Always <see langword="null"/> after transformation — see <see cref="PasswordMigrationOutcome"/>.
/// bcrypt hashes from Node are never carried over verbatim: ASP.NET Identity's
/// <c>PasswordHasher</c> cannot verify against a bcrypt-format hash (task requirement 3;
/// see docs/DATA-MIGRATION.md "Password and refresh-token portability").
/// </param>
/// <param name="MustChangePassword">Whether the migrated user must reset their password on next login.</param>
/// <param name="AzureOid">The optional Azure AD object id, carried over as-is (SSO identity is portable).</param>
/// <param name="IsActive">Whether the account is active.</param>
/// <param name="IsLocked">Whether the account is locked.</param>
/// <param name="FailedAttempts">The failed login attempt counter.</param>
/// <param name="LastLogin">The last successful login timestamp, converted to UTC-based <see cref="DateTimeOffset"/>.</param>
/// <param name="PasswordChangedAt">The last password change timestamp, converted to UTC-based <see cref="DateTimeOffset"/>.</param>
/// <param name="CreatedAtUtc">The original row-creation instant, preserved from source (not the migration run time).</param>
/// <param name="PasswordOutcome">The password-portability decision applied to this row, for the report.</param>
public sealed record MappedUser(
    int SourceId,
    string Email,
    string Name,
    string? Title,
    string? Department,
    string? PasswordHash,
    bool MustChangePassword,
    string? AzureOid,
    bool IsActive,
    bool IsLocked,
    int FailedAttempts,
    DateTimeOffset? LastLogin,
    DateTimeOffset? PasswordChangedAt,
    DateTime CreatedAtUtc,
    PasswordMigrationOutcome PasswordOutcome);
