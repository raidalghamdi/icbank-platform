using System.Globalization;
using System.Text.RegularExpressions;

namespace Icbank.Platform.Application.Dashboard;

/// <summary>
/// Ports the Node <c>parseAnnualDate()</c>/<c>AR_MONTHS</c> free-text date parser verbatim
/// (BUSINESS-RULES.md §9), including its 16-entry Arabic month-name map with alternate spellings.
/// </summary>
public static class ArabicAnnualDateParser
{
    private static readonly Dictionary<string, int> ArabicMonths = new()
    {
        ["يناير"] = 1,
        ["فبراير"] = 2,
        ["مارس"] = 3,
        ["أبريل"] = 4,
        ["ابريل"] = 4,
        ["مايو"] = 5,
        ["يونيو"] = 6,
        ["يوليو"] = 7,
        ["أغسطس"] = 8,
        ["اغسطس"] = 8,
        ["سبتمبر"] = 9,
        ["أكتوبر"] = 10,
        ["اكتوبر"] = 10,
        ["نوفمبر"] = 11,
        ["ديسمبر"] = 12,
    };

    private static readonly Regex ArabicFormat = new(@"^(\d{1,2})\s+(\S+)$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    private static readonly Regex NumericFormat = new(@"^(\d{1,2})-(\d{1,2})$", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

    /// <summary>Parses a free-text annual date in either Arabic (<c>"17 مايو"</c>) or numeric (<c>"MM-DD"</c>) form.</summary>
    /// <param name="raw">The raw, untrusted free-text value.</param>
    /// <returns>The parsed (month, day) pair, or <c>null</c> if the value matches neither known format.</returns>
    public static (int Month, int Day)? Parse(string raw)
    {
        var trimmed = raw.Trim();

        Match arabicMatch = ArabicFormat.Match(trimmed);
        if (arabicMatch.Success)
        {
            var day = int.Parse(arabicMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            if (ArabicMonths.TryGetValue(arabicMatch.Groups[2].Value.Trim(), out var month))
            {
                return (month, day);
            }
        }

        Match numericMatch = NumericFormat.Match(trimmed);
        if (numericMatch.Success)
        {
            var month = int.Parse(numericMatch.Groups[1].Value, CultureInfo.InvariantCulture);
            var day = int.Parse(numericMatch.Groups[2].Value, CultureInfo.InvariantCulture);
            return (month, day);
        }

        return null;
    }
}
