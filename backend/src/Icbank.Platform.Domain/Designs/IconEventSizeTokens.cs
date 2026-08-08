namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// The per-size visual tuning applied to a composed design.
/// </summary>
/// <remarks>
/// Ports <c>SIZE_TOKENS</c> from <c>composer/icon-event-composer.ts</c> verbatim. These are hand-set
/// rather than derived by scaling: a 639×479 mini card needs proportionally larger type and tighter
/// leading than a 3840×2160 master, so a single scale factor produces unreadable output at the
/// extremes.
/// </remarks>
/// <param name="Margin">The outer canvas margin in pixels.</param>
/// <param name="DeptFont">The department tag font size in pixels.</param>
/// <param name="DeptPaddingV">The department tag vertical padding in pixels.</param>
/// <param name="DeptPaddingH">The department tag horizontal padding in pixels.</param>
/// <param name="LogoHeight">The GAC logo height in pixels.</param>
/// <param name="TitleSize">The headline font size in pixels.</param>
/// <param name="SubtitleSize">The subtitle font size in pixels.</param>
/// <param name="MetaFont">The meta chip font size in pixels.</param>
/// <param name="ParagraphGap">The gap between paragraph blocks in pixels.</param>
/// <param name="LineHeight">The body line-height multiplier.</param>
public sealed record IconEventSizeTokens(
    int Margin,
    int DeptFont,
    int DeptPaddingV,
    int DeptPaddingH,
    int LogoHeight,
    int TitleSize,
    int SubtitleSize,
    int MetaFont,
    int ParagraphGap,
    double LineHeight)
{
    private static readonly Dictionary<IconEventSizePreset, IconEventSizeTokens> Catalog = new()
    {
        [IconEventSizePreset.Uhd4k] = new(140, 46, 30, 82, 150, 142, 80, 58, 54, 1.75),
        [IconEventSizePreset.DesktopHd] = new(52, 18, 12, 30, 56, 54, 30, 22, 20, 1.7),
        [IconEventSizePreset.WebStandard] = new(32, 13, 8, 22, 38, 32, 18, 14, 10, 1.55),
        [IconEventSizePreset.WebSmall] = new(22, 10, 5, 15, 26, 24, 14, 11, 7, 1.5),
        [IconEventSizePreset.WebMini] = new(18, 9, 5, 13, 22, 22, 13, 10, 6, 1.5),
    };

    /// <summary>Gets the tokens for a preset.</summary>
    /// <param name="preset">The size preset.</param>
    /// <returns>The preset's design tokens.</returns>
    public static IconEventSizeTokens Resolve(IconEventSizePreset preset) => Catalog[preset];
}
