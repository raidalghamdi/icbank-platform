namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>The 3 named template seed sets the Node source exposes as separate reseed endpoints (BUSINESS-RULES.md §7.1).</summary>
public enum DesignTemplateSeedSet
{
    /// <summary>Presentation-layout templates (paragraphs + 2x2 icon grid).</summary>
    Presentation,

    /// <summary>V2 social-media templates (square/Facebook-cover/Twitter).</summary>
    SocialV2,

    /// <summary>The 2026 template set (institutional/workshop/social).</summary>
    Year2026,
}
