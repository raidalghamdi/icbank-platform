namespace Icbank.Platform.Domain.Designs;

/// <summary>Resolves the pixel dimensions for each <see cref="IconEventSizePreset"/> (ports <c>SIZE_MAP</c> from <c>composer/icon-event-composer.ts</c>, BUSINESS-RULES.md §7.5).</summary>
public static class IconEventSizeCatalog
{
    private static readonly Dictionary<IconEventSizePreset, (int Width, int Height)> Dimensions = new()
    {
        [IconEventSizePreset.Square] = (1200, 1200),
        [IconEventSizePreset.Story] = (1200, 2133),
        [IconEventSizePreset.Landscape] = (2000, 1125),
    };

    /// <summary>Gets the pixel width and height for the given preset.</summary>
    /// <param name="preset">The size preset.</param>
    /// <returns>The width/height tuple in pixels.</returns>
    public static (int Width, int Height) Resolve(IconEventSizePreset preset) => Dimensions[preset];
}
