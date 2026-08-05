namespace Icbank.Platform.DataMigration.Validation;

/// <summary>
/// Generic, pure duplicate-key detector used against any table gaining a new unique index the
/// source data may violate — today that is exactly <c>gac_social_posts(platform, external_id)</c>
/// (AMBIGUOUS-7 / task requirement 3: "detect and report duplicates rather than crashing").
/// </summary>
public static class DuplicateKeyDetector
{
    /// <summary>
    /// Groups <paramref name="items"/> by <paramref name="keySelector"/> and returns every group
    /// with more than one member, so a single unique-index violation is reported as data — not
    /// thrown as an unhandled exception mid-migration.
    /// </summary>
    /// <typeparam name="TItem">The mapped row DTO type.</typeparam>
    /// <typeparam name="TKey">The composite/simple key type the new unique index is defined over.</typeparam>
    /// <param name="items">The full set of mapped rows for one table.</param>
    /// <param name="keySelector">Projects each item to the key the new unique index covers.</param>
    /// <param name="sourceIdSelector">Projects each item to its source-row id, for the report.</param>
    /// <returns>One <see cref="DuplicateKeyGroup{TKey}"/> per key value that appears on more than one row.</returns>
    public static IReadOnlyList<DuplicateKeyGroup<TKey>> FindDuplicates<TItem, TKey>(
        IEnumerable<TItem> items,
        Func<TItem, TKey> keySelector,
        Func<TItem, int> sourceIdSelector)
        where TKey : notnull =>
        items
            .GroupBy(keySelector)
            .Where(group => group.Count() > 1)
            .Select(group => new DuplicateKeyGroup<TKey>(group.Key, group.Select(sourceIdSelector).ToArray()))
            .ToArray();
}
