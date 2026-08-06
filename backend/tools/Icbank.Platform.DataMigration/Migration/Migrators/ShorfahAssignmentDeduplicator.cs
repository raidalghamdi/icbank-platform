using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Resolves legacy duplicate Shorfah assignments that conflict with the target's unique
/// <c>(section_id, user_id)</c> constraint by retaining the latest source write.
/// </summary>
public static class ShorfahAssignmentDeduplicator
{
    /// <summary>Returns source rows to retain and superseded rows, both in source-id order.</summary>
    public static (IReadOnlyList<SourceRow> RowsToMigrate, IReadOnlyList<SourceRow> SupersededRows) SelectLastWrites(
        IEnumerable<SourceRow> sourceRows)
    {
        var rows = sourceRows.ToList();
        var survivingSourceIds = rows
            .GroupBy(row => (SectionId: row.GetInt32("section_id"), UserId: row.GetInt32("user_id")))
            .Select(group => group.Max(row => row.GetInt32("id")))
            .ToHashSet();

        return (
            rows.Where(row => survivingSourceIds.Contains(row.GetInt32("id"))).OrderBy(row => row.GetInt32("id")).ToList(),
            rows.Where(row => !survivingSourceIds.Contains(row.GetInt32("id"))).OrderBy(row => row.GetInt32("id")).ToList());
    }
}
