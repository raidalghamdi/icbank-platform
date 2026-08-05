namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>
/// Port supplying the hardcoded template layout data for the 3 named seed sets (ports
/// <c>composer/seed-presentation.ts</c>, <c>seed-templates-v2.ts</c>, and
/// <c>seed-templates-2026.ts</c>, BUSINESS-RULES.md §7.1). The Node source's seed constants are
/// themselves the source of truth (pixel/percentage layout coordinates, not business logic), so
/// this port is deliberately named and swappable rather than hand-inlined in the command handler
/// -- the default implementation (<c>CuratedDesignTemplateSeedCatalog</c> in Infrastructure)
/// ships every real template name from the Node source with a structurally faithful, simplified
/// layout (see WAVE3B-PORT-NOTES.md for what is and is not reproduced pixel-for-pixel).
/// </summary>
public interface IDesignTemplateSeedCatalog
{
    /// <summary>Returns every template definition belonging to the given seed set, in source order.</summary>
    /// <param name="seedSet">The seed set to retrieve.</param>
    /// <returns>The template definitions.</returns>
    IReadOnlyList<DesignTemplateSeedDefinition> GetSeedSet(DesignTemplateSeedSet seedSet);
}
