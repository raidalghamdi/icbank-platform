namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port for the current instant, injectable for testability and always resolvable into
/// Asia/Riyadh local time via <see cref="RiyadhNow"/>. Introduced to close the Node source's
/// server-local-time cadence bug (BUSINESS-RULES.md §2.1: <c>nextThursday()</c> used naive
/// server-wall-clock time with zero timezone conversion) — every business-date calculation in
/// this port (weekend cadence, "week-start entries this month", etc.) must go through this port
/// instead of <c>DateTime.Now</c>/<c>DateTimeOffset.Now</c>.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>Gets the current instant in UTC.</summary>
    DateTimeOffset UtcNow { get; }

    /// <summary>Gets the current instant converted to Asia/Riyadh local time (UTC+3, no DST).</summary>
    DateTimeOffset RiyadhNow { get; }
}
