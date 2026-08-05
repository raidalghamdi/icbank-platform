namespace Icbank.Platform.DataMigration.Mapping;

/// <summary>
/// Converts raw Postgres timestamp values into the <see cref="DateTimeOffset"/> shape every
/// business-specific date/time column uses (mapped <c>datetimeoffset(3)</c> — e.g.
/// <c>ShorfahSection.ContributedAt</c>, <c>GacSocialPost.PostedAt</c>). Pure function,
/// unit-tested exhaustively — this is the single place the "naive-or-UTC?" decision (task
/// requirement 3) is made, so every table transformer calls through here instead of re-deciding
/// per table. Note: the shared <see cref="Icbank.Platform.Domain.Common.AuditableEntity"/>
/// audit columns (<c>CreatedAt</c>/<c>UpdatedAt</c>/<c>DeletedAt</c>) are a separate case — they
/// are plain UTC <see cref="DateTime"/> mapped to <c>datetime2(3)</c>
/// (<c>AuditableEntityConfigurationExtensions.ConfigureAuditable</c>), not
/// <c>datetimeoffset</c>; transformers set those directly from the same UTC-treated source
/// value without going through this converter.</summary>
/// <remarks>
/// <para><b>Decision (documented in docs/DATA-MIGRATION.md and spec/DATA-MIGRATION-NOTES.md):</b>
/// every timestamp column in the source schema is a Drizzle <c>timestamp(...)</c> column with
/// <b>no timezone</b> (verified against <c>lib/db/src/schema/*.ts</c> — none of the definitions
/// use <c>timestamp(..., { withTimezone: true })</c>). The Node write path
/// (<c>artifacts/api-server</c>) always writes with <c>new Date()</c> or a driver-level
/// <c>defaultNow()</c>; both a JS <c>Date</c> and Postgres' own <c>now()</c> are UTC instants —
/// there is no evidence anywhere in the Node code of a Riyadh-local <c>Date</c> being
/// constructed. Postgres then serializes that UTC instant into the naive <c>timestamp</c> column
/// with no offset attached (the offset is simply dropped, not converted).</para>
/// <para>Therefore: <b>every naive timestamp read from Postgres is treated as UTC</b>, and is
/// converted to a <see cref="DateTimeOffset"/> with offset zero, which SQL Server's
/// <c>datetimeoffset(3)</c> then stores as-is. This is "Asia/Riyadh-correct" in the sense that
/// UTC is unambiguous and every consumer (API, frontend) that needs Riyadh-local wall time
/// converts via <c>IDateTimeProvider.RiyadhNow</c>-style logic at render time, exactly as new
/// platform code does — the migrated data does not need to be pre-rotated to +03:00.</para>
/// <para>This is a judgment call made without a live Postgres/production instance to confirm
/// against — see the "Unverified assumptions" section of docs/DATA-MIGRATION.md.</para>
/// </remarks>
public static class TimestampConverter
{
    private static readonly TimeSpan RiyadhOffset = TimeSpan.FromHours(3);

    /// <summary>
    /// Converts a raw, offset-less Postgres timestamp value (assumed UTC — see remarks) to a
    /// <see cref="DateTimeOffset"/> suitable for a <c>datetimeoffset(3)</c> column, truncated to
    /// millisecond precision to match the destination column's declared scale.
    /// </summary>
    /// <param name="rawUtcTimestamp">The raw timestamp as read from Postgres.</param>
    /// <returns>The equivalent <see cref="DateTimeOffset"/> at UTC (offset zero).</returns>
    public static DateTimeOffset ToDestinationOffset(DateTime rawUtcTimestamp)
    {
        DateTime truncated = TruncateToMilliseconds(rawUtcTimestamp);
        return new DateTimeOffset(DateTime.SpecifyKind(truncated, DateTimeKind.Utc));
    }

    /// <summary>Converts a nullable raw timestamp, passing through <see langword="null"/>.</summary>
    /// <param name="rawUtcTimestamp">The raw nullable timestamp.</param>
    /// <returns>The converted value, or <see langword="null"/>.</returns>
    public static DateTimeOffset? ToDestinationOffset(DateTime? rawUtcTimestamp) =>
        rawUtcTimestamp.HasValue ? ToDestinationOffset(rawUtcTimestamp.Value) : null;

    /// <summary>
    /// Renders a UTC <see cref="DateTimeOffset"/> as its Asia/Riyadh-local wall-clock equivalent,
    /// for report/log display only (never for storage — storage always keeps UTC per above).
    /// </summary>
    /// <param name="value">The UTC-based value to render.</param>
    /// <returns>The same instant, shown at UTC+3.</returns>
    public static DateTimeOffset ToRiyadhDisplay(DateTimeOffset value) => value.ToOffset(RiyadhOffset);

    private static DateTime TruncateToMilliseconds(DateTime value)
    {
        var ticksPerMillisecond = TimeSpan.TicksPerMillisecond;
        var truncatedTicks = value.Ticks - (value.Ticks % ticksPerMillisecond);
        return new DateTime(truncatedTicks, value.Kind);
    }
}
