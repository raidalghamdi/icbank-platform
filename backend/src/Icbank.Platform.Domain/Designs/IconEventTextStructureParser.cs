using System.Text.RegularExpressions;

namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// Reads the shape out of arbitrary source copy: opening lead, list items, labelled sections and a
/// closing line.
/// </summary>
/// <remarks>
/// Staff paste copy written for a document, not for a poster, and the model frequently returns it
/// with the newlines collapsed. A parser that only recognised markers at the start of a line saw
/// <c>احرص على: * مراجعة ... * رفع ...</c> as one unbroken sentence, so the whole policy ran across
/// the canvas as a single paragraph and off both edges. Markers are therefore recognised mid-line
/// as well, which is safe because a bullet glyph is never ordinary Arabic prose.
/// </remarks>
public static class IconEventTextStructureParser
{
    private const int MaxLabelLength = 45;

    private static readonly TimeSpan MatchBudget = TimeSpan.FromSeconds(1);

    private static readonly Regex LineBulletRegex =
        new(@"^\s*(?:[*\-–—•⁃◦▪﹅]|\d{1,2}[.)])\s+(?<body>.+)$", RegexOptions.CultureInvariant, MatchBudget);

    private static readonly Regex InlineBulletRegex =
        new(@"(?:(?<=\s)|^)[*•⁃◦▪﹅]\s+", RegexOptions.CultureInvariant, MatchBudget);

    private static readonly Regex LabelledLineRegex =
        new(@"^(?<label>[^:：\n]{2,45})[:：]\s*(?<body>.*)$", RegexOptions.CultureInvariant, MatchBudget);

    private static readonly Regex SentenceSplitRegex =
        new(@"(?<=[\.؟!۔])\s+", RegexOptions.CultureInvariant, MatchBudget);

    /// <summary>Parses source copy into its structural parts.</summary>
    /// <param name="text">The raw copy, which may be empty, one line, or many paragraphs.</param>
    /// <returns>The parsed structure, never <see langword="null"/>.</returns>
    public static IconEventTextStructure Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return IconEventTextStructure.Empty;
        }

        IReadOnlyList<Segment> segments = SplitIntoSegments(text);
        return Assemble(segments);
    }

    /// <summary>Splits copy into sentences, keeping their terminating punctuation.</summary>
    /// <param name="text">The copy to split.</param>
    /// <returns>The sentences, with surrounding whitespace removed.</returns>
    public static IReadOnlyList<string> SplitSentences(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? Array.Empty<string>()
            : SentenceSplitRegex.Split(text).Select(value => value.Trim()).Where(value => value.Length > 0).ToList();

    private static IconEventTextStructure Assemble(IReadOnlyList<Segment> segments)
    {
        var lead = new List<string>();
        var bullets = new List<string>();
        var sections = new List<IconEventTextSection>();
        string? pendingLabel = null;
        var pendingBody = new List<string>();

        foreach (Segment segment in segments)
        {
            if (segment.IsBullet)
            {
                CloseSection(sections, ref pendingLabel, pendingBody);
                bullets.Add(segment.Text);
                continue;
            }

            Match labelled = LabelledLineRegex.Match(segment.Text);
            if (labelled.Success && IsLabel(labelled.Groups["label"].Value))
            {
                CloseSection(sections, ref pendingLabel, pendingBody);
                pendingLabel = labelled.Groups["label"].Value.Trim();
                AddIfPresent(pendingBody, labelled.Groups["body"].Value);
                continue;
            }

            if (pendingLabel is not null)
            {
                pendingBody.Add(segment.Text);
                continue;
            }

            lead.Add(segment.Text);
        }

        CloseSection(sections, ref pendingLabel, pendingBody);
        return Finalise(lead, bullets, sections);
    }

    private static IconEventTextStructure Finalise(
        List<string> lead,
        List<string> bullets,
        List<IconEventTextSection> sections)
    {
        string? closing = null;
        var structured = bullets.Count > 0 || sections.Count > 0;

        // A trailing one-liner after the substance of the message reads as a sign-off, not as
        // content, so it is kept out of the lead and given the footer slot instead.
        if (structured && lead.Count > 1 && lead[^1].Length <= 160)
        {
            closing = lead[^1];
            lead.RemoveAt(lead.Count - 1);
        }

        var leadText = lead.Count == 0 ? null : string.Join(" ", lead);
        return new IconEventTextStructure(leadText, bullets, sections, closing);
    }

    private static void CloseSection(List<IconEventTextSection> sections, ref string? label, List<string> body)
    {
        if (label is not null)
        {
            sections.Add(new IconEventTextSection(label, string.Join(" ", body).Trim()));
            body.Clear();
        }

        label = null;
    }

    private static void AddIfPresent(List<string> target, string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length > 0)
        {
            target.Add(trimmed);
        }
    }

    private static bool IsLabel(string candidate)
    {
        var trimmed = candidate.Trim();

        // A colon inside running prose (a URL, a time, an aside) is not a heading; only a short
        // standalone phrase is.
        return trimmed.Length is >= 2 and <= MaxLabelLength && !trimmed.Contains('.', StringComparison.Ordinal);
    }

    private static IReadOnlyList<Segment> SplitIntoSegments(string text)
    {
        var segments = new List<Segment>();
        foreach (var rawLine in text.Split('\n'))
        {
            AddLine(segments, rawLine.Trim());
        }

        return segments;
    }

    private static void AddLine(List<Segment> segments, string line)
    {
        if (line.Length == 0)
        {
            return;
        }

        Match lineBullet = LineBulletRegex.Match(line);
        if (lineBullet.Success)
        {
            AddInlineParts(segments, lineBullet.Groups["body"].Value, leadingIsBullet: true);
            return;
        }

        AddInlineParts(segments, line, leadingIsBullet: false);
    }

    private static void AddInlineParts(List<Segment> segments, string line, bool leadingIsBullet)
    {
        var parts = InlineBulletRegex.Split(line).Select(part => part.Trim()).Where(part => part.Length > 0).ToList();
        for (var i = 0; i < parts.Count; i++)
        {
            segments.Add(new Segment(parts[i], leadingIsBullet || i > 0));
        }
    }

    private readonly record struct Segment(string Text, bool IsBullet);
}
