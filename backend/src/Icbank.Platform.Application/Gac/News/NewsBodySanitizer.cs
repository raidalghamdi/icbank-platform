using System.Globalization;
using System.Text;

namespace Icbank.Platform.Application.Gac.News;

/// <summary>Drops article bodies that only restate the headline.</summary>
/// <remarks>
/// A Google News RSS item carries no real summary: its description is the headline with the
/// outlet name appended. Stored verbatim it renders as a second, near-identical line under every
/// card on the media-monitoring page, which reads like a bug next to a curated entry that has a
/// genuine abstract. Anything that adds real prose is kept untouched.
/// </remarks>
public static class NewsBodySanitizer
{
    /// <summary>Returns the body to store, or <see langword="null"/> when it adds nothing.</summary>
    /// <param name="title">The article title.</param>
    /// <param name="body">The candidate body text.</param>
    /// <param name="sourceName">The outlet name, which providers often append to the body.</param>
    /// <returns>The trimmed body, or <see langword="null"/> when it merely restates the title.</returns>
    public static string? Sanitize(string? title, string? body, string? sourceName)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            return null;
        }

        var trimmed = body.Trim();
        var normalizedBody = Normalize(trimmed);
        var normalizedTitle = Normalize(title);

        if (normalizedBody.Length == 0 || normalizedBody == normalizedTitle)
        {
            return null;
        }

        // "<headline> <outlet>" is the standard Google News shape.
        if (!string.IsNullOrWhiteSpace(sourceName))
        {
            var withOutlet = Normalize(title + " " + sourceName);
            if (normalizedBody == withOutlet)
            {
                return null;
            }
        }

        // A body that is the headline plus a handful of trailing characters is still not a summary.
        if (normalizedTitle.Length > 0
            && normalizedBody.StartsWith(normalizedTitle, StringComparison.Ordinal)
            && normalizedBody.Length - normalizedTitle.Length <= 40)
        {
            return null;
        }

        return trimmed;
    }

    /// <summary>Reduces text to comparable form: no punctuation, collapsed whitespace, lowercase.</summary>
    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(value.Length);
        var lastWasSpace = true;
        foreach (var ch in value.Trim())
        {
            if (char.IsWhiteSpace(ch))
            {
                if (!lastWasSpace)
                {
                    builder.Append(' ');
                    lastWasSpace = true;
                }

                continue;
            }

            if (char.IsPunctuation(ch) || char.IsSymbol(ch))
            {
                continue;
            }

            builder.Append(char.ToLower(ch, CultureInfo.InvariantCulture));
            lastWasSpace = false;
        }

        return builder.ToString().TrimEnd();
    }
}
