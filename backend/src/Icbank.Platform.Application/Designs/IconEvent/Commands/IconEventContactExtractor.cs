using System.Text.RegularExpressions;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Ports the Node source's independent, code-side email/phone extraction (BUSINESS-RULES.md
/// §7.4 rule 3): run once by the AI per the prompt, and independently again in code via regex
/// directly against the raw text -- the code-extracted value always wins when found.
/// </summary>
public static class IconEventContactExtractor
{
    private static readonly Regex EmailPattern = new(@"[a-zA-Z0-9._+-]+@[a-zA-Z0-9-]+(?:\.[a-zA-Z0-9-]+)+", RegexOptions.Compiled);
    private static readonly Regex PhonePattern = new(@"(?:\+?\d[\d\s\-]{7,}\d)|(?:\b0\d{8,}\b)", RegexOptions.Compiled);

    /// <summary>Extracts the literal contact email from the raw text, falling back to the AI's extraction.</summary>
    /// <param name="rawFull">The full raw event text.</param>
    /// <param name="aiExtractedEmail">The AI's extracted email, used only if the regex finds none.</param>
    /// <returns>The final contact email, or empty string if neither source found one.</returns>
    public static string ExtractEmail(string rawFull, string? aiExtractedEmail)
    {
        Match match = EmailPattern.Match(rawFull);
        return match.Success ? match.Value : (aiExtractedEmail ?? string.Empty).Trim();
    }

    /// <summary>Extracts the literal contact phone from the raw text, falling back to the AI's extraction.</summary>
    /// <param name="rawFull">The full raw event text.</param>
    /// <param name="aiExtractedPhone">The AI's extracted phone, used only if the regex finds none.</param>
    /// <returns>The final contact phone, or empty string if neither source found one.</returns>
    public static string ExtractPhone(string rawFull, string? aiExtractedPhone)
    {
        Match match = PhonePattern.Match(rawFull);
        return match.Success ? match.Value : (aiExtractedPhone ?? string.Empty).Trim();
    }
}
