using System.Globalization;
using System.Text.RegularExpressions;

namespace Icbank.Platform.Domain.Designs;

/// <summary>Chooses catalogue icons that match what a piece of copy is actually about.</summary>
/// <remarks>
/// Icon names arriving from the model are suggestions, not guarantees: unknown names used to fall
/// through to a decorative <c>sparkles</c> tile, which is how a policy notice about attendance ended
/// up illustrated with a four-pointed star. Every name is therefore validated against the catalogue
/// and, when it fails, replaced by scoring the copy against each icon's own keywords rather than by
/// picking a fixed default.
/// </remarks>
public static class IconEventIconResolver
{
    private const string LastResortIcon = "megaphone";

    private static readonly TimeSpan MatchBudget = TimeSpan.FromSeconds(1);

    private static readonly Regex WordRegex = new(@"[\p{L}\p{N}]+", RegexOptions.CultureInvariant, MatchBudget);

    /// <summary>Picks the icon that best represents a piece of copy.</summary>
    /// <param name="preferred">The name suggested upstream, used when the catalogue recognises it.</param>
    /// <param name="text">The copy to analyse when the suggestion is unusable.</param>
    /// <param name="exclude">Icon names already used elsewhere in the composition.</param>
    /// <returns>A name that always exists in <see cref="IconLibrary"/>.</returns>
    public static string Resolve(string? preferred, string? text, IReadOnlySet<string>? exclude = null)
    {
        if (IsUsable(preferred, exclude))
        {
            return preferred!.Trim();
        }

        return Score(text, exclude) ?? Fallback(exclude);
    }

    /// <summary>Picks a set of distinct supporting icons for a composition.</summary>
    /// <param name="preferred">The names suggested upstream, in priority order.</param>
    /// <param name="texts">Per-slot copy used to fill any remaining slots semantically.</param>
    /// <param name="count">How many icons the layout needs.</param>
    /// <param name="reserved">Icon names already spoken for, such as the main icon.</param>
    /// <returns>Exactly <paramref name="count"/> distinct catalogue names.</returns>
    public static IReadOnlyList<string> ResolveMany(
        IEnumerable<string>? preferred,
        IReadOnlyList<string> texts,
        int count,
        IReadOnlySet<string>? reserved = null)
    {
        ArgumentNullException.ThrowIfNull(texts);
        var used = new HashSet<string>(reserved ?? new HashSet<string>(StringComparer.Ordinal), StringComparer.Ordinal);
        var chosen = new List<string>(count);

        foreach (var candidate in preferred ?? Enumerable.Empty<string>())
        {
            if (chosen.Count == count)
            {
                break;
            }

            if (IsUsable(candidate, used))
            {
                var name = candidate.Trim();
                chosen.Add(name);
                used.Add(name);
            }
        }

        FillRemaining(chosen, texts, count, used);
        return chosen;
    }

    private static void FillRemaining(List<string> chosen, IReadOnlyList<string> texts, int count, HashSet<string> used)
    {
        var slot = 0;
        while (chosen.Count < count)
        {
            var text = slot < texts.Count ? texts[slot] : null;
            var name = Score(text, used) ?? Fallback(used);
            chosen.Add(name);
            used.Add(name);
            slot++;
        }
    }

    private static bool IsUsable(string? name, IReadOnlySet<string>? exclude) =>
        !string.IsNullOrWhiteSpace(name)
        && IconLibrary.ValidNames.Contains(name.Trim())
        && exclude?.Contains(name.Trim()) != true;

    private static string? Score(string? text, IReadOnlySet<string>? exclude)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        HashSet<string> words = Tokenise(text);
        if (words.Count == 0)
        {
            return null;
        }

        IconDefinition? best = null;
        var bestScore = 0;
        foreach (IconDefinition icon in IconLibrary.All)
        {
            if (exclude?.Contains(icon.Name) == true)
            {
                continue;
            }

            var score = ScoreIcon(icon, words);
            if (score > bestScore)
            {
                bestScore = score;
                best = icon;
            }
        }

        return best?.Name;
    }

    private static int ScoreIcon(IconDefinition icon, IReadOnlyCollection<string> words)
    {
        var score = 0;
        foreach (var keyword in Vocabulary(icon))
        {
            var normalised = Normalise(keyword);
            if (normalised.Length == 0)
            {
                continue;
            }

            // An exact word match is a far stronger signal than a substring hit, which in Arabic
            // fires constantly on shared roots and prefixes.
            if (words.Contains(normalised))
            {
                score += 3;
            }
            else if (normalised.Length >= 4 && words.Any(word => word.Contains(normalised, StringComparison.Ordinal)))
            {
                score += 1;
            }
        }

        return score;
    }

    private static IEnumerable<string> Vocabulary(IconDefinition icon) =>
        IconEventKeywordIndex.Supplementary.TryGetValue(icon.Name, out IReadOnlyList<string>? extra)
            ? icon.Keywords.Concat(extra)
            : icon.Keywords;

    private static HashSet<string> Tokenise(string text) =>
        WordRegex.Matches(text).Select(match => Normalise(match.Value)).Where(word => word.Length > 1).ToHashSet(StringComparer.Ordinal);

    private static string Normalise(string value)
    {
        var lowered = value.Trim().ToLower(CultureInfo.InvariantCulture);
        var builder = new System.Text.StringBuilder(lowered.Length);
        foreach (var c in lowered)
        {
            builder.Append(Fold(c));
        }

        return builder.ToString();
    }

    /// <summary>Folds the Arabic letter variants that differ only by orthography.</summary>
    /// <param name="c">The character to fold.</param>
    /// <returns>The canonical form, or the original character.</returns>
    /// <remarks>
    /// Staff copy mixes أ/إ/آ with ا and ة with ه freely, and the catalogue keywords use only one
    /// spelling each, so unfolded comparison misses most real matches.
    /// </remarks>
    private static char Fold(char c) => c switch
    {
        'أ' or 'إ' or 'آ' or 'ٱ' => 'ا',
        'ة' => 'ه',
        'ى' => 'ي',
        'ؤ' => 'و',
        'ئ' => 'ي',
        _ => c,
    };

    private static string Fallback(IReadOnlySet<string>? exclude) =>
        exclude?.Contains(LastResortIcon) == true
            ? IconLibrary.All.First(icon => !exclude.Contains(icon.Name)).Name
            : LastResortIcon;
}
