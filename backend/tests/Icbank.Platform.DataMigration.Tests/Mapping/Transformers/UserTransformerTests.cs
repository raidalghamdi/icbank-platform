using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Tests.Fixtures;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping.Transformers;

/// <summary>
/// Exhaustive unit tests for <see cref="UserTransformer"/> against realistic fixture rows shaped
/// like the actual <c>users</c> table (DATA-MODEL.md §3.1) -- covering the password-portability
/// decision (task requirement 3), SSO-only users, and every nullable column.
/// </summary>
public sealed class UserTransformerTests
{
    [Fact]
    public void Transform_UserWithBcryptPassword_MarksNotPortableAndMustChangePassword()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedUser result = UserTransformer.Transform(row);

        result.PasswordHash.Should().BeNull();
        result.MustChangePassword.Should().BeTrue();
        result.PasswordOutcome.Should().Be(PasswordMigrationOutcome.BcryptHashNotPortableMustReset);
    }

    [Fact]
    public void Transform_SsoOnlyUserWithNullPasswordHash_DoesNotRequirePasswordReset()
    {
        Dictionary<string, object?> values = BaseRow();
        values["password_hash"] = null;
        values["azure_oid"] = "00000000-0000-0000-0000-000000000001";
        SourceRow row = SourceRowFixture.Build(values);

        MappedUser result = UserTransformer.Transform(row);

        result.PasswordHash.Should().BeNull();
        result.MustChangePassword.Should().BeFalse();
        result.PasswordOutcome.Should().Be(PasswordMigrationOutcome.SsoOnlyNoPasswordToMigrate);
        result.AzureOid.Should().Be("00000000-0000-0000-0000-000000000001");
    }

    [Fact]
    public void Transform_SsoOnlyUserWithEmptyStringPasswordHash_TreatedAsNoPassword()
    {
        // Hard case: some legacy rows may have an empty string rather than a true SQL NULL.
        Dictionary<string, object?> values = BaseRow();
        values["password_hash"] = string.Empty;
        SourceRow row = SourceRowFixture.Build(values);

        MappedUser result = UserTransformer.Transform(row);

        result.MustChangePassword.Should().BeFalse();
        result.PasswordOutcome.Should().Be(PasswordMigrationOutcome.SsoOnlyNoPasswordToMigrate);
    }

    [Fact]
    public void Transform_MapsAllScalarFieldsCorrectly()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedUser result = UserTransformer.Transform(row);

        result.SourceId.Should().Be(7);
        result.Email.Should().Be("sara.alqahtani@icbank.example");
        result.Name.Should().Be("Sara Al-Qahtani");
        result.Title.Should().Be("Senior Analyst");
        result.Department.Should().Be("Media");
        result.IsActive.Should().BeTrue();
        result.IsLocked.Should().BeFalse();
        result.FailedAttempts.Should().Be(0);
    }

    [Fact]
    public void Transform_OptionalTitleAndDepartmentNull_MapToNull()
    {
        Dictionary<string, object?> values = BaseRow();
        values["title"] = null;
        values["department"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedUser result = UserTransformer.Transform(row);

        result.Title.Should().BeNull();
        result.Department.Should().BeNull();
    }

    [Fact]
    public void Transform_FailedAttemptsNull_DefaultsToZero()
    {
        Dictionary<string, object?> values = BaseRow();
        values["failed_attempts"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedUser result = UserTransformer.Transform(row);

        result.FailedAttempts.Should().Be(0);
    }

    [Fact]
    public void Transform_LastLoginNull_MapsToNullDateTimeOffset()
    {
        Dictionary<string, object?> values = BaseRow();
        values["last_login"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedUser result = UserTransformer.Transform(row);

        result.LastLogin.Should().BeNull();
    }

    [Fact]
    public void Transform_LastLoginPresent_ConvertsToUtcOffsetZero()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedUser result = UserTransformer.Transform(row);

        result.LastLogin.Should().NotBeNull();
        result.LastLogin!.Value.Offset.Should().Be(TimeSpan.Zero);
        result.LastLogin.Value.Should().Be(new DateTimeOffset(2026, 7, 1, 6, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Transform_PasswordChangedAtNull_MapsToNull()
    {
        Dictionary<string, object?> values = BaseRow();
        values["password_changed_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedUser result = UserTransformer.Transform(row);

        result.PasswordChangedAt.Should().BeNull();
    }

    [Fact]
    public void Transform_CreatedAtPreservedFromSourceNotMigrationRunTime()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedUser result = UserTransformer.Transform(row);

        result.CreatedAtUtc.Should().Be(new DateTime(2023, 5, 10, 12, 0, 0));
    }

    [Fact]
    public void Transform_CreatedAtMissing_ThrowsBecauseSourceAlwaysSetsIt()
    {
        Dictionary<string, object?> values = BaseRow();
        values.Remove("created_at");
        SourceRow row = SourceRowFixture.Build(values);

        Action act = () => UserTransformer.Transform(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Transform_LockedAccount_MapsIsLockedTrue()
    {
        Dictionary<string, object?> values = BaseRow();
        values["is_locked"] = true;
        SourceRow row = SourceRowFixture.Build(values);

        MappedUser result = UserTransformer.Transform(row);

        result.IsLocked.Should().BeTrue();
    }

    private static Dictionary<string, object?> BaseRow() => new()
    {
        ["id"] = 7,
        ["email"] = "sara.alqahtani@icbank.example",
        ["name"] = "Sara Al-Qahtani",
        ["title"] = "Senior Analyst",
        ["department"] = "Media",
        ["password_hash"] = "$2b$10$abcdefghijklmnopqrstuv",
        ["azure_oid"] = null,
        ["is_active"] = true,
        ["is_locked"] = false,
        ["failed_attempts"] = 0,
        ["last_login"] = new DateTime(2026, 7, 1, 6, 0, 0),
        ["password_changed_at"] = new DateTime(2026, 1, 1, 0, 0, 0),
        ["created_at"] = new DateTime(2023, 5, 10, 12, 0, 0),
    };
}
