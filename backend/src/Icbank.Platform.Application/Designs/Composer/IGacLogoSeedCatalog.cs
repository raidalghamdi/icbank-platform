namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>
/// Port supplying the official GAC brand-logo seed assets (ports <c>composer/seed-gac-assets.ts</c>,
/// BUSINESS-RULES.md §7). The Node source embeds two ~500KB base64 PNGs sourced from the brand
/// manual; reproducing that exact bitmap data is out of scope for this port (see
/// WAVE3B-PORT-NOTES.md) -- the default implementation ships the same 2 real logo names with a
/// tiny valid 1x1 PNG placeholder so the idempotent-seed/upload/storage pipeline is fully
/// exercisable end-to-end without embedding proprietary brand artwork in source control.
/// </summary>
public interface IGacLogoSeedCatalog
{
    /// <summary>Returns every official GAC logo definition, in source order.</summary>
    /// <returns>The logo definitions.</returns>
    IReadOnlyList<GacLogoSeedDefinition> GetLogos();
}
