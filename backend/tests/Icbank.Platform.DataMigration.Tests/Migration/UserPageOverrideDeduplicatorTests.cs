using FluentAssertions;
using Icbank.Platform.DataMigration.Migration.Migrators;
using Icbank.Platform.DataMigration.Source;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.DataMigration.Tests.Migration;

/// <summary>
/// Regression coverage for the documented source-data repair rule for
/// <c>user_page_overrides</c>.
/// </summary>
public sealed class UserPageOverrideDeduplicatorTests
{
    [Fact]
    public void SelectLastWrites_ExactDuplicate_KeepsHighestIdAndRejectsLowerId()
    {
        SourceRow[] rows = new[]
        {
            Row(10, 1, 2, 3, OverrideGrantType.Allow),
            Row(11, 1, 2, 3, OverrideGrantType.Allow),
        };

        (IReadOnlyList<SourceRow> rowsToMigrate, IReadOnlyList<SourceRow> supersededRows) = UserPageOverrideDeduplicator.SelectLastWrites(rows);

        rowsToMigrate.Select(row => row["id"]).Should().Equal(11);
        supersededRows.Select(row => row["id"]).Should().Equal(10);
    }

    [Fact]
    public void SelectLastWrites_ContradictoryAllowThenDeny_KeepsLaterDeny()
    {
        SourceRow[] rows = new[]
        {
            Row(20, 1, 2, 3, OverrideGrantType.Allow),
            Row(21, 1, 2, 3, OverrideGrantType.Deny),
        };

        (IReadOnlyList<SourceRow> rowsToMigrate, IReadOnlyList<SourceRow> supersededRows) = UserPageOverrideDeduplicator.SelectLastWrites(rows);

        rowsToMigrate.Should().ContainSingle();
        rowsToMigrate.Single()["grant_type"].Should().Be("deny");
        supersededRows.Select(row => row["id"]).Should().Equal(20);
    }

    [Fact]
    public void SelectLastWrites_ContradictoryDenyThenAllow_KeepsLaterAllow()
    {
        SourceRow[] rows = new[]
        {
            Row(30, 1, 2, 3, OverrideGrantType.Deny),
            Row(31, 1, 2, 3, OverrideGrantType.Allow),
        };

        (IReadOnlyList<SourceRow> rowsToMigrate, IReadOnlyList<SourceRow> supersededRows) = UserPageOverrideDeduplicator.SelectLastWrites(rows);

        rowsToMigrate.Should().ContainSingle();
        rowsToMigrate.Single()["grant_type"].Should().Be("allow");
        supersededRows.Select(row => row["id"]).Should().Equal(30);
    }

    [Fact]
    public void SelectLastWrites_NoDuplicate_PreservesEveryRow()
    {
        SourceRow[] rows = new[]
        {
            Row(42, 1, 2, 3, OverrideGrantType.Allow),
            Row(40, 1, 2, 4, OverrideGrantType.Deny),
            Row(41, 1, 3, 3, OverrideGrantType.Allow),
        };

        (IReadOnlyList<SourceRow> rowsToMigrate, IReadOnlyList<SourceRow> supersededRows) = UserPageOverrideDeduplicator.SelectLastWrites(rows);

        rowsToMigrate.Select(row => row["id"]).Should().Equal(40, 41, 42);
        supersededRows.Should().BeEmpty();
    }

    private static SourceRow Row(int id, int userId, int pageId, int permissionId, OverrideGrantType grantType) =>
        new(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["user_id"] = userId,
            ["page_id"] = pageId,
            ["permission_id"] = permissionId,
            ["grant_type"] = grantType.ToString().ToLowerInvariant(),
        });
}
