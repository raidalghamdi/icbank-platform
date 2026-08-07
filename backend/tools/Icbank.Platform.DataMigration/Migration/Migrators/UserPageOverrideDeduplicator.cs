using Icbank.Platform.DataMigration.Mapping;
using Icbank.Platform.DataMigration.Source;

namespace Icbank.Platform.DataMigration.Migration.Migrators;

/// <summary>
/// Selects the authoritative source row for each user/page/permission override key.
/// </summary>
public static class UserPageOverrideDeduplicator
{
    /// <summary>
    /// Applies the migration's explicit last-write-wins rule: for every duplicate
    /// <c>(user_id, page_id, permission_id)</c> group, preserves the row with the highest source
    /// <c>id</c> and returns every lower-id row as a superseded rejection.
    /// </summary>
    /// <param name="rows">The source rows to evaluate.</param>
    /// <returns>
    /// Rows to migrate and rows to count as superseded duplicate rejections, both ordered by
    /// source <c>id</c>.
    /// </returns>
    public static (IReadOnlyList<SourceRow> RowsToMigrate, IReadOnlyList<SourceRow> SupersededRows) SelectLastWrites(
        IEnumerable<SourceRow> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);

        var candidates = rows
            .Select(row => new
            {
                Row = row,
                SourceId = row.GetInt32("id"),
                Key = (
                    UserId: row.GetInt32("user_id"),
                    PageId: row.GetInt32("page_id"),
                    PermissionId: row.GetInt32("permission_id")),
            })
            .ToList();

        var survivingSourceIds = candidates
            .GroupBy(candidate => candidate.Key)
            .Select(group => group.Max(candidate => candidate.SourceId))
            .ToHashSet();

        return (
            candidates
                .Where(candidate => survivingSourceIds.Contains(candidate.SourceId))
                .OrderBy(candidate => candidate.SourceId)
                .Select(candidate => candidate.Row)
                .ToList(),
            candidates
                .Where(candidate => !survivingSourceIds.Contains(candidate.SourceId))
                .OrderBy(candidate => candidate.SourceId)
                .Select(candidate => candidate.Row)
                .ToList());
    }
}
