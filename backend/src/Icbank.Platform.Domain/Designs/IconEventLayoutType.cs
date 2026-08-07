namespace Icbank.Platform.Domain.Designs;

/// <summary>The 4 layout variants for icon-event designs (BUSINESS-RULES.md §7.4/§7.5).</summary>
public enum IconEventLayoutType
{
    /// <summary>Hero layout with a headline stats panel, used when the input contains numbers.</summary>
    StatsHero,

    /// <summary>Hero layout without stats.</summary>
    Hero,

    /// <summary>Grid layout that foregrounds stats.</summary>
    Grid,

    /// <summary>Split two-column layout.</summary>
    Split,

    /// <summary>Text-only layout with no main icon (BUSINESS-RULES.md §7.4 rule 5: every design set must include one).</summary>
    Typography,
}
