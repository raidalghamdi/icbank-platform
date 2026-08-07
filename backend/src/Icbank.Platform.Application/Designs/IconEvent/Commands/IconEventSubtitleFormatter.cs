using System.Text.RegularExpressions;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Ports the Node source's subtitle-construction algorithm (BUSINESS-RULES.md §7.4 rule 4):
/// strips the headline line and contact email/phone from the raw text, then applies
/// bullet-preserving paragraph normalization (lines starting with a bullet marker are kept
/// separate; everything else is merged into flowing paragraphs).
/// </summary>
public static class IconEventSubtitleFormatter
{
    private const int LongParagraphThreshold = 50;

    private static readonly Regex BulletPattern = new(@"^\s*[*\-•⁃◦▪﹅]\s+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex TrailingColon = new(@"[:\uFF1A]\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>Builds the subtitle from raw text, stripping the headline and contact info, then normalizing paragraphs.</summary>
    /// <param name="rawFull">The full raw event text.</param>
    /// <param name="finalHeadline">The already-resolved final headline, to strip from the top of the text.</param>
    /// <param name="contactEmail">The extracted contact email to strip from the body.</param>
    /// <param name="contactPhone">The extracted contact phone to strip from the body.</param>
    /// <returns>The formatted subtitle text.</returns>
    public static string Build(string rawFull, string finalHeadline, string? contactEmail, string? contactPhone)
    {
        var cleaned = StripHeadlineLine(rawFull, finalHeadline);
        if (!string.IsNullOrEmpty(contactEmail))
        {
            cleaned = cleaned.Replace(contactEmail, string.Empty).Trim();
        }

        if (!string.IsNullOrEmpty(contactPhone))
        {
            cleaned = cleaned.Replace(contactPhone, string.Empty).Trim();
        }

        return NormalizeParagraphs(cleaned);
    }

    private static string StripHeadlineLine(string rawFull, string finalHeadline)
    {
        if (rawFull.StartsWith(finalHeadline, StringComparison.Ordinal))
        {
            return rawFull[finalHeadline.Length..].Trim();
        }

        var lines = rawFull.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        var headlinePrefix = finalHeadline.Length > 15 ? finalHeadline[..15] : finalHeadline;
        if (lines.Count > 1 && (lines[0] == finalHeadline || lines[0].Contains(headlinePrefix, StringComparison.Ordinal)))
        {
            return string.Join('\n', lines.Skip(1)).Trim();
        }

        return rawFull;
    }

    private static string NormalizeParagraphs(string cleanedRaw)
    {
        var withoutCr = cleanedRaw.Replace("\r", string.Empty);
        var paragraphs = Regex.Split(withoutCr, @"\n{2,}");
        IEnumerable<string> normalized = paragraphs.Select(NormalizeParagraph).Where(p => p.Length > 0);
        return string.Join("\n\n", normalized);
    }

    private static string NormalizeParagraph(string paragraph)
    {
        var lines = paragraph.Split('\n').Select(l => l.Trim()).Where(l => l.Length > 0).ToList();
        if (lines.Count == 0)
        {
            return string.Empty;
        }

        return lines.Any(l => BulletPattern.IsMatch(l))
            ? string.Join('\n', lines.Select(l => Regex.Replace(l, @"\s{2,}", " ").Trim()))
            : MergeFlowingParagraph(paragraph);
    }

    private static string MergeFlowingParagraph(string paragraph)
    {
        var merged = Regex.Replace(paragraph, @"\n+", " ");
        merged = Regex.Replace(merged, @"\s{2,}", " ").Trim();
        return merged.Length > LongParagraphThreshold && TrailingColon.IsMatch(merged)
            ? TrailingColon.Replace(merged, string.Empty).Trim()
            : merged;
    }
}
