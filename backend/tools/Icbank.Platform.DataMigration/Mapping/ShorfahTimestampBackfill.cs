namespace Icbank.Platform.DataMigration.Mapping;

/// <summary>
/// Chooses a defensible, documented default for the Shorfah workflow timestamp columns
/// (<c>contributed_at</c>, <c>reviewed_at</c>, <c>approved_at</c>, <c>sla_starts_at</c>) that were
/// nullable in Postgres but are non-null <c>datetimeoffset(3)</c> columns after the port
/// (AMBIGUOUS-8 in DATA-MODEL.md / DOMAIN-PORT-NOTES.md §5). Pure function — every decision is
/// deterministic given the same inputs, and every backfilled row is reported so nobody mistakes
/// a backfilled value for a real historical timestamp.
/// </summary>
/// <remarks>
/// <para><b>Decision (see docs/DATA-MIGRATION.md):</b> for a given row, in priority order:</para>
/// <list type="number">
/// <item><description>use the column's own value if it is not null;</description></item>
/// <item><description>otherwise use the earliest non-null sibling workflow timestamp on the same
/// row (e.g. if <c>contributed_at</c> is null but <c>reviewed_at</c> or <c>approved_at</c> is
/// set, a contribution must logically have preceded them);</description></item>
/// <item><description>otherwise fall back to the parent section's own <c>created_at</c>, which
/// is never null;</description></item>
/// <item><description>as an absolute last resort (should not occur given (3) always exists),
/// fall back to the supplied migration-run timestamp.</description></item>
/// </list>
/// <para>Every row that required step 2, 3, or 4 is flagged as "backfilled" in the returned
/// result so the report can list exactly which rows received a synthetic value instead of a
/// real historical one.</para>
/// </remarks>
public static class ShorfahTimestampBackfill
{
    /// <summary>
    /// Resolves the value to store for one nullable workflow timestamp column.
    /// </summary>
    /// <param name="ownValue">The column's own raw value, if present.</param>
    /// <param name="siblingValues">Other workflow timestamps on the same row, in preference order.</param>
    /// <param name="sectionCreatedAt">The owning section's own non-null <c>created_at</c>, used as the fallback of last resort before the run timestamp.</param>
    /// <param name="migrationRunTimestamp">The migration run's start time, used only if every other source is unavailable.</param>
    /// <returns>The resolved value and whether it was backfilled (synthetic) rather than a genuine source value.</returns>
    public static BackfillResult Resolve(
        DateTime? ownValue,
        IReadOnlyList<DateTime?> siblingValues,
        DateTime sectionCreatedAt,
        DateTime migrationRunTimestamp)
    {
        if (ownValue.HasValue)
        {
            return new BackfillResult(ownValue.Value, WasBackfilled: false);
        }

        DateTime? earliestSibling = siblingValues.Where(v => v.HasValue).Select(v => v!.Value).OrderBy(v => v).FirstOrDefault();
        if (earliestSibling.HasValue && earliestSibling.Value != default)
        {
            return new BackfillResult(earliestSibling.Value, WasBackfilled: true);
        }

        if (sectionCreatedAt != default)
        {
            return new BackfillResult(sectionCreatedAt, WasBackfilled: true);
        }

        return new BackfillResult(migrationRunTimestamp, WasBackfilled: true);
    }

    /// <summary>The outcome of resolving one nullable workflow timestamp.</summary>
    /// <param name="Value">The value to store.</param>
    /// <param name="WasBackfilled">Whether <paramref name="Value"/> is synthetic rather than the source's own value.</param>
    public sealed record BackfillResult(DateTime Value, bool WasBackfilled);
}
