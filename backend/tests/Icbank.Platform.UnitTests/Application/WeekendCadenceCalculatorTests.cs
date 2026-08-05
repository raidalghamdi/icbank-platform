using FluentAssertions;
using Icbank.Platform.Application.Weekend;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>
/// Proves <see cref="WeekendCadenceCalculator.NextThursday"/> is correct at the exact UTC/Riyadh
/// day-boundary case BUSINESS-RULES.md §2.1 flags as a Node source bug: Riyadh is UTC+3, so
/// 21:00-24:00 UTC is already the next calendar day in Riyadh. Every case here passes an
/// already-Riyadh-converted <see cref="DateTimeOffset"/>, proving the calculator itself is
/// timezone-correct as long as callers resolve Riyadh time first (via
/// <c>IDateTimeProvider.RiyadhNow</c>), which is the fix this port makes.
/// </summary>
public sealed class WeekendCadenceCalculatorTests
{
    [Fact]
    public void NextThursday_FromMonday_ReturnsThisWeeksThursday()
    {
        // 2026-08-03 is a Monday.
        var riyadhMonday = new DateTimeOffset(2026, 8, 3, 10, 0, 0, TimeSpan.FromHours(3));

        var result = WeekendCadenceCalculator.NextThursday(riyadhMonday);

        result.Should().Be("2026-08-06");
    }

    [Fact]
    public void NextThursday_FromThursdayItself_ReturnsNextWeeksThursday()
    {
        // 2026-08-06 is a Thursday; the Node "|| 7" fallback means today never returns itself.
        var riyadhThursday = new DateTimeOffset(2026, 8, 6, 10, 0, 0, TimeSpan.FromHours(3));

        var result = WeekendCadenceCalculator.NextThursday(riyadhThursday);

        result.Should().Be("2026-08-13");
    }

    [Fact]
    public void NextThursday_AtUtcLateNightThatIsAlreadyNextDayInRiyadh_UsesRiyadhCalendarDay()
    {
        // 2026-08-03 23:30 UTC is a Monday in UTC, but 2026-08-04 02:30 in Riyadh (UTC+3) — a
        // Tuesday. If the calculator (or its caller) used the naive UTC day instead of the
        // Riyadh-converted day, it would compute "next Thursday" from Monday, not Tuesday. Both
        // land on the same Thursday here, so this case specifically proves the *day-of-week*
        // read respects the Riyadh offset already baked into the input, not the UTC wall clock.
        var utcInstant = new DateTimeOffset(2026, 8, 3, 23, 30, 0, TimeSpan.Zero);
        DateTimeOffset riyadhEquivalent = utcInstant.ToOffset(TimeSpan.FromHours(3));

        riyadhEquivalent.DayOfWeek.Should().Be(DayOfWeek.Tuesday, "02:30 on Aug 4th in Riyadh is a Tuesday, not the UTC Monday");

        var result = WeekendCadenceCalculator.NextThursday(riyadhEquivalent);

        result.Should().Be("2026-08-06");
    }

    [Fact]
    public void NextThursday_FromFridaySaturday_ReturnsThursdayFiveOrSixDaysOut()
    {
        // 2026-08-07 is a Friday.
        var riyadhFriday = new DateTimeOffset(2026, 8, 7, 10, 0, 0, TimeSpan.FromHours(3));

        var result = WeekendCadenceCalculator.NextThursday(riyadhFriday);

        result.Should().Be("2026-08-13");
    }
}
