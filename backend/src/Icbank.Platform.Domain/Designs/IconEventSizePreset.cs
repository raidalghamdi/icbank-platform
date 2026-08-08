namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// The official output sizes offered by the icon-event designer.
/// </summary>
/// <remarks>
/// These are the five presets the Node platform shipped in <c>composer/icon-event-composer.ts</c>
/// and exposed through the studio endpoint. They replace the earlier social trio
/// (square/story/landscape): the designer now picks a style first and a set of output sizes
/// second, so the social aspect ratios are no longer part of the size choice.
/// </remarks>
public enum IconEventSizePreset
{
    /// <summary>3840×2160 (16:9 UHD) 4K master.</summary>
    Uhd4k,

    /// <summary>1440×864 (5:3) desktop HD.</summary>
    DesktopHd,

    /// <summary>1067×712 (3:2) web and email.</summary>
    WebStandard,

    /// <summary>799×479 (5:3) small web; logo and department are suppressed at this size.</summary>
    WebSmall,

    /// <summary>639×479 (4:3) mini web; logo and department are suppressed at this size.</summary>
    WebMini,
}
