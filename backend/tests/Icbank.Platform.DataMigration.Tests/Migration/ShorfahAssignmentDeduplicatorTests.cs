using FluentAssertions;
using Icbank.Platform.DataMigration.Migration.Migrators;
using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Tests.Migration;

/// <summary>Regression coverage for the source repair needed by the target assignment unique key.</summary>
public sealed class ShorfahAssignmentDeduplicatorTests
{
    [Fact]
    public void SelectLastWrites_DuplicateSectionUser_KeepsHighestId()
    {
        SourceRow[] rows =
        {
            Row(7, 5, 3),
            Row(8, 5, 3),
            Row(9, 6, 3),
        };

        (IReadOnlyList<SourceRow> rowsToMigrate, IReadOnlyList<SourceRow> supersededRows) =
            ShorfahAssignmentDeduplicator.SelectLastWrites(rows);

        rowsToMigrate.Select(row => row["id"]).Should().Equal(8, 9);
        supersededRows.Select(row => row["id"]).Should().Equal(7);
    }

    [Fact]
    public void SelectLastWrites_NoDuplicate_PreservesEveryRow()
    {
        SourceRow[] rows =
        {
            Row(3, 4, 1),
            Row(2, 4, 2),
        };

        (IReadOnlyList<SourceRow> rowsToMigrate, IReadOnlyList<SourceRow> supersededRows) =
            ShorfahAssignmentDeduplicator.SelectLastWrites(rows);

        rowsToMigrate.Select(row => row["id"]).Should().Equal(2, 3);
        supersededRows.Should().BeEmpty();
    }

    private static SourceRow Row(int id, int sectionId, int userId) =>
        new(new Dictionary<string, object?>
        {
            ["id"] = id,
            ["section_id"] = sectionId,
            ["user_id"] = userId,
        });
}
