using System.Globalization;

namespace Icbank.Platform.Application.Weekend;

/// <summary>
/// Ports the Node <c>nextThursday()</c> cadence rule (BUSINESS-RULES.md §2.1) but takes an
/// already-resolved Asia/Riyadh local instant as input instead of reading the server clock
/// directly. This is a deliberate behaviour fix vs. the Node source, which computed
/// <c>new Date().getDay()</c> on naive server-local (usually UTC) time — near the UTC/Riyadh day
/// boundary this could target the wrong Thursday. Callers must pass
/// <see cref="Common.Interfaces.IDateTimeProvider.RiyadhNow"/>, never <see cref="DateTimeOffset.Now"/>.
/// </summary>
public static class WeekendCadenceCalculator
{
    private const int ThursdayDayOfWeek = 4; // System.DayOfWeek: Sunday=0 ... Thursday=4
    private const int DaysPerWeek = 7;

    /// <summary>Computes the ISO (<c>yyyy-MM-dd</c>) date string of the next Thursday strictly after <paramref name="riyadhLocalNow"/>'s calendar day.</summary>
    /// <param name="riyadhLocalNow">The current instant, already converted to Asia/Riyadh local time.</param>
    /// <returns>The ISO date string of the next Thursday (never today, matching the Node source's <c>|| 7</c> fallback).</returns>
    public static string NextThursday(DateTimeOffset riyadhLocalNow)
    {
        DateTime today = riyadhLocalNow.Date;
        var currentDayOfWeek = (int)today.DayOfWeek;
        var daysUntilThursday = ((ThursdayDayOfWeek - currentDayOfWeek) + DaysPerWeek) % DaysPerWeek;
        if (daysUntilThursday == 0)
        {
            daysUntilThursday = DaysPerWeek;
        }

        return today.AddDays(daysUntilThursday).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }
}
