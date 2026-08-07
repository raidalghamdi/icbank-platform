namespace Icbank.Platform.Application.Shorfah;

/// <summary>The 12 Arabic month names, ported verbatim from the several duplicated <c>arabicMonths</c> arrays in <c>shorfah.ts</c> (BUSINESS-RULES.md §1.7, §1.9).</summary>
public static class ArabicMonthNames
{
    private static readonly string[] Names =
    {
        "يناير", "فبراير", "مارس", "أبريل", "مايو", "يونيو",
        "يوليو", "أغسطس", "سبتمبر", "أكتوبر", "نوفمبر", "ديسمبر",
    };

    /// <summary>Gets the Arabic name for the given 1-based calendar month, or the numeric fallback if out of range.</summary>
    /// <param name="month">The 1-based calendar month.</param>
    /// <returns>The Arabic month name, or the month number as a string if out of the valid [1,12] range.</returns>
    public static string For(int month) => month is >= 1 and <= 12 ? Names[month - 1] : month.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
