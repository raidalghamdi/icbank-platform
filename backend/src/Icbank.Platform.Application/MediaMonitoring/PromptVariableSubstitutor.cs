using System.Text.RegularExpressions;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Substitutes <c>{{key}}</c> placeholders in a prompt template with caller-supplied variable
/// values (<c>POST /prompts/:id/run</c>). Unmatched placeholders are left verbatim, matching the
/// Node source's simple string-replace behaviour.
/// </summary>
public static partial class PromptVariableSubstitutor
{
    /// <summary>Substitutes every <c>{{key}}</c> placeholder found in <paramref name="variables"/>.</summary>
    /// <param name="promptText">The template text containing <c>{{key}}</c> placeholders.</param>
    /// <param name="variables">The substitution map.</param>
    /// <returns>The prompt text with all matched placeholders replaced.</returns>
    public static string Substitute(string promptText, IReadOnlyDictionary<string, string> variables) =>
        PlaceholderPattern().Replace(promptText, match =>
        {
            var key = match.Groups[1].Value.Trim();
            return variables.TryGetValue(key, out var value) ? value : match.Value;
        });

    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}")]
    private static partial Regex PlaceholderPattern();
}
