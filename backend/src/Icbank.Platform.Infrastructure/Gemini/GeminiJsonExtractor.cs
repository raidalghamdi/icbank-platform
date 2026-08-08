namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Ports the Node source's JSON-robustness heuristics verbatim: (1) strip a leading/trailing
/// ```` ```json ```` or ```` ``` ```` markdown fence, (2) find the first <c>{</c> or <c>[</c> and
/// drop any preamble text before it, (3) on parse failure, walk the string tracking brace/bracket
/// depth to find the last complete top-level element and truncate there
/// (<c>repairTruncatedJson</c>), closing any still-open braces/brackets.
/// </summary>
public static class GeminiJsonExtractor
{
    /// <summary>Strips markdown code-fences and any preamble before the first JSON token.</summary>
    /// <param name="rawText">The raw model output.</param>
    /// <returns>The text with fences stripped and truncated to start at the first <c>{</c>/<c>[</c>, or the original trimmed text if neither is found.</returns>
    public static string StripFencesAndPreamble(string rawText)
    {
        var text = rawText.Trim();
        text = StripFence(text, "```json");
        text = StripFence(text, "```");

        var firstBrace = text.IndexOf('{');
        var firstBracket = text.IndexOf('[');
        var start = FirstNonNegative(firstBrace, firstBracket);
        return start < 0 ? text.Trim() : text[start..].Trim();
    }

    /// <summary>
    /// Attempts to repair truncated JSON by tracking bracket/brace depth character-by-character,
    /// truncating at the last position where a top-level element cleanly ended, and closing any
    /// remaining open containers.
    /// </summary>
    /// <param name="text">The (already fence-stripped) JSON text that failed to parse.</param>
    /// <returns>A best-effort repaired string; may still fail to parse for sufficiently malformed input.</returns>
    public static string RepairTruncated(string text)
    {
        var state = new GeminiJsonRepairState();
        var lastCompleteElementEnd = -1;

        for (var i = 0; i < text.Length; i++)
        {
            if (state.Advance(text[i]))
            {
                lastCompleteElementEnd = i;
            }
        }

        if (lastCompleteElementEnd < 0)
        {
            return text;
        }

        var truncated = text[..(lastCompleteElementEnd + 1)].TrimEnd();
        if (truncated.EndsWith(','))
        {
            truncated = truncated[..^1];
        }

        return truncated + Rescan(truncated).CloseRemaining();
    }

    private static GeminiJsonRepairState Rescan(string text)
    {
        var state = new GeminiJsonRepairState();
        foreach (var c in text)
        {
            state.Advance(c);
        }

        return state;
    }

    private static string StripFence(string text, string fenceMarker)
    {
        var trimmed = text.Trim();
        if (!trimmed.StartsWith(fenceMarker, StringComparison.Ordinal))
        {
            return text;
        }

        var withoutStart = trimmed[fenceMarker.Length..];
        var closingIndex = withoutStart.LastIndexOf("```", StringComparison.Ordinal);
        return closingIndex >= 0 ? withoutStart[..closingIndex] : withoutStart;
    }

    private static int FirstNonNegative(int a, int b)
    {
        if (a < 0)
        {
            return b;
        }

        if (b < 0)
        {
            return a;
        }

        return Math.Min(a, b);
    }
}
