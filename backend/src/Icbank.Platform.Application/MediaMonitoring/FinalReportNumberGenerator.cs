using System.Globalization;
using System.Text.RegularExpressions;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Computes the next official report number in the <c>GAC-MEDIA-{n}/{year}</c> format
/// (BUSINESS-RULES.md §5.2), by scanning existing report numbers for the current year and taking
/// <c>max(n)+1</c>. Carries over the Node source's race-condition caveat verbatim (DATA-MODEL.md
/// §3.7): this is not safe under concurrent report creation, matching Shorfah's <c>issueNo</c>
/// pattern exactly -- flagged, not silently fixed, per WAVE3A-PORT-NOTES.md.
/// </summary>
public static partial class FinalReportNumberGenerator
{
    private const int FirstSequenceNumber = 1;

    /// <summary>Computes the next report number for the given year from a set of existing report numbers.</summary>
    /// <param name="existingReportNumbers">Every existing report number, from any year.</param>
    /// <param name="year">The year to generate a number for.</param>
    /// <returns>The next report number, e.g. <c>GAC-MEDIA-22/2026</c>.</returns>
    public static string Next(IEnumerable<string> existingReportNumbers, int year)
    {
        var yearSuffix = "/" + year.ToString(CultureInfo.InvariantCulture);
        var maxSequence = existingReportNumbers
            .Where(n => n.EndsWith(yearSuffix, StringComparison.Ordinal))
            .Select(ExtractSequenceNumber)
            .Where(n => n.HasValue)
            .Select(n => n!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return $"GAC-MEDIA-{maxSequence + FirstSequenceNumber}/{year.ToString(CultureInfo.InvariantCulture)}";
    }

    private static int? ExtractSequenceNumber(string reportNumber)
    {
        Match match = SequencePattern().Match(reportNumber);
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    [GeneratedRegex(@"GAC-MEDIA-(\d+)/\d+")]
    private static partial Regex SequencePattern();
}
