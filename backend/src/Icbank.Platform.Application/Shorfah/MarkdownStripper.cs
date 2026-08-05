using System.Text.RegularExpressions;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Strips markdown syntax to plain text, ported verbatim from <c>stripMd()</c> in
/// <c>shorfah.ts:1130-1138</c> (BUSINESS-RULES.md §1.9). This is a real fidelity loss vs. the
/// PDF/HTML exports (headings/bold/lists from <c>ContentMd</c> are not preserved as document
/// formatting, only as flattened paragraphs) -- carried over deliberately, not silently fixed,
/// matching the Node source's documented behaviour exactly.
/// </summary>
public static partial class MarkdownStripper
{
    /// <summary>Strips markdown syntax from the given text, leaving flattened plain text.</summary>
    /// <param name="markdown">The markdown source, or <c>null</c>.</param>
    /// <returns>The plain-text result, or an empty string if <paramref name="markdown"/> was <c>null</c> or empty.</returns>
    public static string Strip(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
        {
            return string.Empty;
        }

        var result = ImagePattern().Replace(markdown, string.Empty);
        result = LinkPattern().Replace(result, "$1");
        result = EmphasisCharsPattern().Replace(result, string.Empty);
        result = ExcessBlankLinesPattern().Replace(result, "\n\n");
        return result.Trim();
    }

    [GeneratedRegex(@"!\[[^\]]*\]\([^)]+\)")]
    private static partial Regex ImagePattern();

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)")]
    private static partial Regex LinkPattern();

    [GeneratedRegex(@"[*_`~#>]")]
    private static partial Regex EmphasisCharsPattern();

    [GeneratedRegex(@"\n{3,}")]
    private static partial Regex ExcessBlankLinesPattern();
}
