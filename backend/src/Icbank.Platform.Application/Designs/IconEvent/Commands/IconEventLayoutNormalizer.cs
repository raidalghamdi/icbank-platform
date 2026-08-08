using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Ports the Node source's layout-diversity-enforcement rule (BUSINESS-RULES.md §7.4 rule 5):
/// downgrades <c>stats-hero</c> to <c>hero</c> when the input has no numbers, forces a
/// diversity-guaranteeing fallback triplet if the AI proposed 3 identical layouts, and guarantees
/// at least one <c>typography</c> (text-only) layout among the 3.
/// </summary>
public static class IconEventLayoutNormalizer
{
    private static readonly Dictionary<string, IconEventLayoutType> LayoutsByKey = new(StringComparer.OrdinalIgnoreCase)
    {
        ["stats-hero"] = IconEventLayoutType.StatsHero,
        ["hero"] = IconEventLayoutType.Hero,
        ["grid"] = IconEventLayoutType.Grid,
        ["split"] = IconEventLayoutType.Split,
        ["typography"] = IconEventLayoutType.Typography,
    };

    /// <summary>Normalizes the 3 AI-proposed layout keys per the diversity/typography-guarantee rules.</summary>
    /// <param name="proposedLayouts">The 3 raw layout keys proposed by the AI (already defaulted to "hero" for unknown keys).</param>
    /// <param name="hasNumbersInInput">Whether the source text contains any digit.</param>
    /// <returns>The 3 final, diversity-guaranteed layout types.</returns>
    public static IReadOnlyList<IconEventLayoutType> Normalize(IReadOnlyList<string> proposedLayouts, bool hasNumbersInInput)
    {
        var layouts = proposedLayouts
            .Select(key => LayoutsByKey.TryGetValue(key, out IconEventLayoutType layout) ? layout : IconEventLayoutType.Hero)
            .ToList();

        for (var i = 0; i < layouts.Count; i++)
        {
            if (!hasNumbersInInput && layouts[i] == IconEventLayoutType.StatsHero)
            {
                layouts[i] = IconEventLayoutType.Hero;
            }
        }

        if (layouts.Count == 3 && layouts[0] == layouts[1] && layouts[1] == layouts[2])
        {
            layouts = hasNumbersInInput
                ? new List<IconEventLayoutType> { IconEventLayoutType.StatsHero, IconEventLayoutType.Split, IconEventLayoutType.Typography }
                : new List<IconEventLayoutType> { IconEventLayoutType.Hero, IconEventLayoutType.Split, IconEventLayoutType.Typography };
        }

        if (!layouts.Contains(IconEventLayoutType.Typography) && layouts.Count == 3)
        {
            layouts[2] = IconEventLayoutType.Typography;
        }

        return layouts;
    }

    /// <summary>Resolves a single wire-format layout key, falling back to <c>hero</c>.</summary>
    /// <param name="key">The kebab-case layout key; may be null or unknown.</param>
    /// <returns>The matching layout type, or <see cref="IconEventLayoutType.Hero"/>.</returns>
    public static IconEventLayoutType ToLayout(string? key) =>
        key is not null && LayoutsByKey.TryGetValue(key, out IconEventLayoutType layout) ? layout : IconEventLayoutType.Hero;

    /// <summary>Converts a layout type back to its wire-format kebab-case key.</summary>
    /// <param name="layout">The layout type.</param>
    /// <returns>The kebab-case key, e.g. <c>stats-hero</c>.</returns>
    public static string ToKey(IconEventLayoutType layout) =>
        layout switch
        {
            IconEventLayoutType.StatsHero => "stats-hero",
            IconEventLayoutType.Hero => "hero",
            IconEventLayoutType.Grid => "grid",
            IconEventLayoutType.Split => "split",
            IconEventLayoutType.Typography => "typography",
            _ => "hero",
        };
}
