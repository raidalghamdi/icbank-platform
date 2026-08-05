using System.Text.RegularExpressions;

namespace Icbank.Platform.Application.Weekend;

/// <summary>
/// Ports the Node <c>updateStyleProfile()</c> derived-style computation verbatim
/// (BUSINESS-RULES.md §2.5): average paragraph word count, first-10 opener sentences, last-10
/// closer sentences, top-20 recurring non-stopword Arabic keywords against the source's exact
/// 24-word stopword list, and the quote-usage classifier with the source's exact thresholds
/// (<c>count &gt; entries*2</c> → dense, <c>count &gt; entries</c> → moderate, else limited).
/// </summary>
public static class StyleProfileRecalculator
{
    private const int OpenerCloserSampleSize = 10;
    private const int TopKeywordCount = 20;
    private const int MinKeywordLength = 3;
    private const int MinParagraphLength = 20;
    private const string ToneSummaryDefault = "رسمية مبسطة";
    private const string QuoteUsageDense = "كثيف";
    private const string QuoteUsageModerate = "معتدل";
    private const string QuoteUsageLimited = "محدود";

    private static readonly HashSet<string> ArabicStopwords = new()
    {
        "في", "من", "إلى", "على", "عن", "مع", "هذا", "هذه", "التي", "الذي",
        "وفي", "كما", "قد", "لا", "ما", "إن", "أن", "كان", "أو", "ولا",
        "هو", "هي", "نحن", "أنا", "لم", "وهو", "وهي", "وقد", "ثم", "بين",
    };

    private static readonly string[] QuoteTriggerWords = { "قال", "روي", "قرآن", "آية", "حديث", "تعالى" };
    private static readonly Regex NonArabicCharacters = new(@"[^\u0600-\u06FF]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    private static readonly Regex SentenceSeparators = new(@"[.!?؟]", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    private static readonly Regex ParagraphSeparators = new(@"\n+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));
    private static readonly Regex WhitespaceSeparators = new(@"\s+", RegexOptions.Compiled, TimeSpan.FromMilliseconds(200));

    /// <summary>Recomputes the style profile from the full archive corpus.</summary>
    /// <param name="entryBodies">Every archive entry's body text.</param>
    /// <returns>The recomputed style-profile fields, or <c>null</c> if the archive is empty (matches the Node source's early-return).</returns>
    public static StyleProfileComputation? Recompute(IReadOnlyList<string> entryBodies)
    {
        if (entryBodies.Count == 0)
        {
            return null;
        }

        var allText = string.Join("\n\n", entryBodies);
        var avgParagraphLength = ComputeAverageParagraphLength(allText);
        var openers = entryBodies.Select(ExtractOpener).Where(s => !string.IsNullOrEmpty(s)).Take(OpenerCloserSampleSize).ToList();
        var closers = entryBodies.Select(ExtractCloser).Where(s => !string.IsNullOrEmpty(s)).Take(OpenerCloserSampleSize).ToList();
        List<string> keywords = ExtractTopKeywords(allText);
        var quoteUsage = ClassifyQuoteUsage(allText, entryBodies.Count);

        return new StyleProfileComputation(ToneSummaryDefault, avgParagraphLength, openers, closers, keywords, quoteUsage);
    }

    private static float ComputeAverageParagraphLength(string allText)
    {
        var paragraphs = ParagraphSeparators.Split(allText).Where(p => p.Trim().Length > MinParagraphLength).ToList();
        if (paragraphs.Count == 0)
        {
            return 0f;
        }

        return (float)paragraphs.Average(p => WhitespaceSeparators.Split(p.Trim()).Length);
    }

    private static string ExtractOpener(string body)
    {
        var sentences = SentenceSeparators.Split(body);
        return sentences.Length > 0 ? sentences[0].Trim() : string.Empty;
    }

    private static string ExtractCloser(string body)
    {
        var sentences = SentenceSeparators.Split(body).Where(s => s.Trim().Length > 0).ToList();
        return sentences.Count > 0 ? sentences[^1].Trim() : string.Empty;
    }

    private static List<string> ExtractTopKeywords(string allText)
    {
        var frequency = new Dictionary<string, int>();
        foreach (var word in WhitespaceSeparators.Split(allText))
        {
            var clean = NonArabicCharacters.Replace(word, string.Empty).Trim();
            if (clean.Length > MinKeywordLength && !ArabicStopwords.Contains(clean))
            {
                frequency[clean] = frequency.GetValueOrDefault(clean) + 1;
            }
        }

        return frequency.OrderByDescending(pair => pair.Value).Take(TopKeywordCount).Select(pair => pair.Key).ToList();
    }

    private static string ClassifyQuoteUsage(string allText, int entryCount)
    {
        var quoteCount = QuoteTriggerWords.Sum(trigger => Regex.Matches(allText, trigger, RegexOptions.None, TimeSpan.FromMilliseconds(200)).Count);

        if (quoteCount > entryCount * 2)
        {
            return QuoteUsageDense;
        }

        return quoteCount > entryCount ? QuoteUsageModerate : QuoteUsageLimited;
    }
}
