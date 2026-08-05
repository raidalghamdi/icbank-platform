using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Mapping.Transformers;

/// <summary>
/// Pure transformer from a raw <c>users</c> <see cref="SourceRow"/> to a <see cref="MappedUser"/>.
/// Applies the password-portability decision (task requirement 3) and the UTC timestamp
/// convention (<see cref="TimestampConverter"/>). No I/O, no EF Core — fully unit testable with
/// fixture rows built directly from the Postgres schema documented in DATA-MODEL.md §3.1.
/// </summary>
public static class UserTransformer
{
    /// <summary>Transforms one raw <c>users</c> row.</summary>
    /// <param name="row">The raw source row.</param>
    /// <returns>The mapped, destination-ready DTO.</returns>
    public static MappedUser Transform(SourceRow row)
    {
        string? sourcePasswordHash = row.GetNullableString("password_hash");
        bool hadPassword = !string.IsNullOrEmpty(sourcePasswordHash);

        DateTime createdAtRaw = row.GetRawTimestamp("created_at")
            ?? throw new InvalidOperationException("users.created_at was null — source data is expected to always set this column.");

        PasswordMigrationOutcome passwordOutcome = hadPassword
            ? PasswordMigrationOutcome.BcryptHashNotPortableMustReset
            : PasswordMigrationOutcome.SsoOnlyNoPasswordToMigrate;

        return new MappedUser(
            SourceId: row.GetInt32("id"),
            Email: row.GetString("email"),
            Name: row.GetString("name"),
            Title: row.GetNullableString("title"),
            Department: row.GetNullableString("department"),
            PasswordHash: null, // Never carried over -- see PasswordMigrationOutcome remarks.
            MustChangePassword: hadPassword,
            AzureOid: row.GetNullableString("azure_oid"),
            IsActive: row.GetBoolean("is_active"),
            IsLocked: row.GetBoolean("is_locked"),
            FailedAttempts: row.GetNullableInt32("failed_attempts") ?? 0,
            LastLogin: TimestampConverter.ToDestinationOffset(row.GetRawTimestamp("last_login")),
            PasswordChangedAt: TimestampConverter.ToDestinationOffset(row.GetRawTimestamp("password_changed_at")),
            CreatedAtUtc: createdAtRaw,
            PasswordOutcome: passwordOutcome);
    }
}
