namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// Resolves pixel dimensions, wire names, and chrome rules for each <see cref="IconEventSizePreset"/>.
/// </summary>
/// <remarks>
/// Ports <c>SIZE_MAP</c> and <c>isMiniSize</c> from <c>composer/icon-event-composer.ts</c>. The wire
/// names are hyphenated (<c>uhd-4k</c>, <c>web-standard</c>), which <see cref="Enum.Parse{TEnum}(string)"/>
/// cannot round-trip, so every string boundary must go through <see cref="TryParse"/> and
/// <see cref="ToWireValue"/> rather than the enum parser.
/// </remarks>
public static class IconEventSizeCatalog
{
    private static readonly Dictionary<IconEventSizePreset, IconEventSizeSpec> Specs = new()
    {
        [IconEventSizePreset.Uhd4k] = new("uhd-4k", 3840, 2160, "16:9 UHD", "4K UHD", "٤K فائق الدقة"),
        [IconEventSizePreset.DesktopHd] = new("desktop-hd", 1440, 864, "5:3", "Desktop HD", "سطح المكتب HD"),
        [IconEventSizePreset.WebStandard] = new("web-standard", 1067, 712, "3:2", "Web / Email", "ويب / بريد إلكتروني"),
        [IconEventSizePreset.WebSmall] = new("web-small", 799, 479, "5:3", "Small", "صغير"),
        [IconEventSizePreset.WebMini] = new("web-mini", 639, 479, "4:3", "Mini", "مصغّر"),
    };

    private static readonly Dictionary<string, IconEventSizePreset> ByWireValue =
        Specs.ToDictionary(pair => pair.Value.WireValue, pair => pair.Key, StringComparer.OrdinalIgnoreCase);

    /// <summary>Gets every preset in display order.</summary>
    public static IReadOnlyList<IconEventSizePreset> All { get; } = Specs.Keys.ToArray();

    /// <summary>Gets the accepted wire values, for validator messages.</summary>
    public static IReadOnlyCollection<string> WireValues { get; } = Specs.Values.Select(s => s.WireValue).ToArray();

    /// <summary>Gets the full specification for a preset.</summary>
    /// <param name="preset">The size preset.</param>
    /// <returns>The preset's specification.</returns>
    public static IconEventSizeSpec Resolve(IconEventSizePreset preset) => Specs[preset];

    /// <summary>Gets the pixel width and height for a preset.</summary>
    /// <param name="preset">The size preset.</param>
    /// <returns>The width/height tuple in pixels.</returns>
    public static (int Width, int Height) Dimensions(IconEventSizePreset preset)
    {
        IconEventSizeSpec spec = Specs[preset];
        return (spec.Width, spec.Height);
    }

    /// <summary>Gets the hyphenated wire value a client sends for a preset.</summary>
    /// <param name="preset">The size preset.</param>
    /// <returns>The wire value, for example <c>web-standard</c>.</returns>
    public static string ToWireValue(IconEventSizePreset preset) => Specs[preset].WireValue;

    /// <summary>Resolves a client-supplied wire value to a preset.</summary>
    /// <param name="value">The candidate wire value; may be null or unknown.</param>
    /// <param name="preset">The resolved preset when the value is recognised.</param>
    /// <returns><see langword="true"/> when the value maps to a known preset.</returns>
    public static bool TryParse(string? value, out IconEventSizePreset preset)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            return ByWireValue.TryGetValue(value.Trim(), out preset);
        }

        preset = default;
        return false;
    }

    /// <summary>
    /// Indicates whether a preset is too small to carry the GAC logo and the department tag.
    /// </summary>
    /// <param name="preset">The size preset.</param>
    /// <returns><see langword="true"/> for the small and mini web sizes.</returns>
    public static bool SuppressesChrome(IconEventSizePreset preset) =>
        preset is IconEventSizePreset.WebSmall or IconEventSizePreset.WebMini;
}
