using FluentAssertions;
using Icbank.Platform.DataMigration.Mapping.Dtos;
using Icbank.Platform.DataMigration.Mapping.Transformers;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.DataMigration.Tests.Fixtures;
using Xunit;

namespace Icbank.Platform.DataMigration.Tests.Mapping.Transformers;

/// <summary>
/// Exhaustive unit tests for <see cref="UserRoleTransformer"/>, including the multi-role
/// migration decision (task requirement 3: every row migrates, not just the first via the
/// Node <c>.limit(1)</c> behavior) and the <c>assigned_at</c>/<c>created_at</c> fallback.
/// </summary>
public sealed class UserRoleTransformerTests
{
    [Fact]
    public void Transform_MapsAllFields()
    {
        SourceRow row = SourceRowFixture.Build(BaseRow());

        MappedUserRole result = UserRoleTransformer.Transform(row);

        result.SourceId.Should().Be(100);
        result.UserSourceId.Should().Be(7);
        result.RoleSourceId.Should().Be(2);
        result.AssignedBySourceId.Should().Be(1);
        result.AssignedAtUtc.Should().Be(new DateTime(2024, 4, 1, 0, 0, 0));
    }

    [Fact]
    public void Transform_AssignedByNull_MapsToNull()
    {
        // Hard case: a role could have been assigned by a system/seed process, not a human user.
        Dictionary<string, object?> values = BaseRow();
        values["assigned_by"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        MappedUserRole result = UserRoleTransformer.Transform(row);

        result.AssignedBySourceId.Should().BeNull();
    }

    [Fact]
    public void Transform_AssignedAtNullFallsBackToCreatedAt()
    {
        Dictionary<string, object?> values = BaseRow();
        values["assigned_at"] = null;
        values["created_at"] = new DateTime(2023, 1, 1, 0, 0, 0);
        SourceRow row = SourceRowFixture.Build(values);

        MappedUserRole result = UserRoleTransformer.Transform(row);

        result.AssignedAtUtc.Should().Be(new DateTime(2023, 1, 1, 0, 0, 0));
    }

    [Fact]
    public void Transform_BothAssignedAtAndCreatedAtNull_Throws()
    {
        Dictionary<string, object?> values = BaseRow();
        values["assigned_at"] = null;
        values["created_at"] = null;
        SourceRow row = SourceRowFixture.Build(values);

        Action act = () => UserRoleTransformer.Transform(row);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Transform_SameUserTwoDifferentRoleRows_BothTransformIndependently()
    {
        // Demonstrates the multi-role decision at the transformer level: two rows for the same
        // user_id produce two independent MappedUserRole DTOs, neither dropped.
        Dictionary<string, object?> firstRoleValues = BaseRow();
        Dictionary<string, object?> secondRoleValues = BaseRow();
        secondRoleValues["id"] = 101;
        secondRoleValues["role_id"] = 5;

        MappedUserRole first = UserRoleTransformer.Transform(SourceRowFixture.Build(firstRoleValues));
        MappedUserRole second = UserRoleTransformer.Transform(SourceRowFixture.Build(secondRoleValues));

        first.UserSourceId.Should().Be(second.UserSourceId);
        first.RoleSourceId.Should().NotBe(second.RoleSourceId);
        first.SourceId.Should().NotBe(second.SourceId);
    }

    private static Dictionary<string, object?> BaseRow() => new()
    {
        ["id"] = 100,
        ["user_id"] = 7,
        ["role_id"] = 2,
        ["assigned_by"] = 1,
        ["assigned_at"] = new DateTime(2024, 4, 1, 0, 0, 0),
        ["created_at"] = new DateTime(2024, 4, 1, 0, 0, 0),
    };
}
