namespace Icbank.Platform.Domain.Designs;

/// <summary>Shortens copy to a character budget without cutting a word in half.</summary>
public static class IconEventTextTrimmer
{
    private const char Ellipsis = '…';

    /// <summary>Trims copy to a budget, preferring a sentence break and falling back to a word break.</summary>
    /// <param name="text">The copy to shorten; may be null or empty.</param>
    /// <param name="maxChars">The inclusive character budget. Non-positive budgets discard the copy.</param>
    /// <returns>The shortened copy, or <see langword="null"/> when nothing survives.</returns>
    public static string? Trim(string? text, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(text) || maxChars <= 0)
        {
            return null;
        }

        var value = Collapse(text);
        return value.Length <= maxChars ? value : Shorten(value, maxChars);
    }

    /// <summary>Collapses all runs of whitespace into single spaces.</summary>
    /// <param name="text">The copy to normalise.</param>
    /// <returns>The single-spaced copy.</returns>
    public static string Collapse(string text) =>
        string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));

    private static string Shorten(string value, int maxChars)
    {
        // Ending on a complete sentence reads as an intentional summary; ending mid-clause reads as
        // a bug, so a sentence break anywhere in the back half of the budget is preferred.
        var window = value[..maxChars];
        var sentenceEnd = window.LastIndexOfAny(new[] { '.', '؟', '!', '۔' });
        if (sentenceEnd >= maxChars / 2)
        {
            return window[..(sentenceEnd + 1)].TrimEnd();
        }

        var lastSpace = window.LastIndexOf(' ');
        var cut = lastSpace > 0 ? window[..lastSpace] : window;
        return cut.TrimEnd(' ', '،', ',', '-', '–') + Ellipsis;
    }
}
