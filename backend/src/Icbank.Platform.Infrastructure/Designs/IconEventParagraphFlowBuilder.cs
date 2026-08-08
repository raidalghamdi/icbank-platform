using System.Text.RegularExpressions;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>Builds the source-compatible body flow before values are encoded into HTML.</summary>
internal static class IconEventParagraphFlowBuilder
{
    private static readonly Regex BulletLineRegex = new(@"^\s*[*\-•⁃◦▪﹅]\s+(.+)$", RegexOptions.CultureInvariant);

    private static readonly Regex EmailMentionRegex = new(@"(البريد\s*الإلكتروني|email|e-?mail|إيميل)\s*[:ـ\-：]?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex PhoneMentionRegex = new(@"(الهاتف|رقم\s*التواصل|phone|tel|جوال)\s*[:ـ\-：]?\s*$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    private static readonly Regex SentenceRegex = new(@"(?<=[\.؟!۔])\s+", RegexOptions.CultureInvariant);

    private static readonly Regex SubHeadColonRegex = new(@"^[^\n]{3,45}[:：]\s*$", RegexOptions.CultureInvariant);

    private static readonly Regex SubHeadQuestionRegex = new(@"^[^\n]{3,80}؟\s*$", RegexOptions.CultureInvariant);

    internal static IconEventParagraphFlow Build(string? subtitle, string? email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(subtitle))
        {
            return new IconEventParagraphFlow(Array.Empty<IconEventParagraphBlock>(), false, false);
        }

        var lines = subtitle.Split('\n').Select(line => line.TrimEnd()).ToArray();
        var nonEmpty = lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
        var structured = nonEmpty.Any(line => BulletLineRegex.IsMatch(line)) || nonEmpty.Any(IsSubHeading);
        return structured ? BuildStructured(lines, email, phone) : BuildSimple(subtitle, email, phone);
    }

    internal static bool IsSubHeading(string line)
    {
        var value = line.Trim();
        return value.Length > 0 && !BulletLineRegex.IsMatch(value) && (SubHeadQuestionRegex.IsMatch(value) || SubHeadColonRegex.IsMatch(value));
    }

    internal static bool IsEmailMention(string value) => EmailMentionRegex.IsMatch(value);

    internal static bool IsPhoneMention(string value) => PhoneMentionRegex.IsMatch(value);

    private static IconEventParagraphFlow BuildSimple(string subtitle, string? email, string? phone)
    {
        var accumulator = new IconEventParagraphAccumulator(email, phone);
        foreach (var paragraph in SplitIntoParagraphs(subtitle))
        {
            accumulator.AddText(paragraph);
        }

        return accumulator.ToFlow();
    }

    private static IconEventParagraphFlow BuildStructured(IEnumerable<string> lines, string? email, string? phone)
    {
        var accumulator = new IconEventParagraphAccumulator(email, phone);
        foreach (var rawLine in lines)
        {
            AddStructuredLine(accumulator, rawLine.Trim());
        }

        accumulator.FlushText();
        accumulator.FlushBullets();
        return accumulator.ToFlow();
    }

    private static void AddStructuredLine(IconEventParagraphAccumulator accumulator, string line)
    {
        Match bullet = BulletLineRegex.Match(line);
        if (line.Length == 0)
        {
            accumulator.FlushText();
            accumulator.FlushBullets();
        }
        else if (bullet.Success)
        {
            accumulator.FlushText();
            accumulator.AddBullet(bullet.Groups[1].Value.Trim());
        }
        else if (IsSubHeading(line))
        {
            accumulator.FlushText();
            accumulator.FlushBullets();
            accumulator.AddSubHeading(line);
        }
        else
        {
            accumulator.FlushBullets();
            accumulator.AddTextLine(line);
        }
    }

    private static IReadOnlyList<string> SplitIntoParagraphs(string text)
    {
        var paragraphs = text.Split("\n", StringSplitOptions.None).Select(value => value.Trim()).Where(value => value.Length > 0).ToList();
        if (paragraphs.Count > 1)
        {
            return paragraphs;
        }

        var sentences = SentenceRegex.Split(text).Select(value => value.Trim()).Where(value => value.Length > 0).ToList();
        return SplitSentences(text, sentences);
    }

    private static IReadOnlyList<string> SplitSentences(string text, IReadOnlyList<string> sentences)
    {
        if (sentences.Count <= 1)
        {
            return new[] { text };
        }

        if (sentences.Count <= 2)
        {
            return sentences;
        }

        var midpoint = (int)Math.Ceiling(sentences.Count / 2d);
        return new[] { string.Join(" ", sentences.Take(midpoint)), string.Join(" ", sentences.Skip(midpoint)) };
    }
}
