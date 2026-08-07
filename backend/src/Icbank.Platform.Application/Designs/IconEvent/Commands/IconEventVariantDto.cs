namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>One returned design variant, after all post-processing rules have been applied.</summary>
/// <param name="Id">The variant slot id, e.g. <c>variant-1</c>.</param>
/// <param name="Layout">The final layout key.</param>
/// <param name="MainIcon">The final main icon name.</param>
/// <param name="SupportingIcons">The final supporting icon names.</param>
/// <param name="ColorScheme">Always <c>teal</c> per the official-identity rule.</param>
/// <param name="Headline">The final headline.</param>
/// <param name="Subtitle">The final subtitle.</param>
/// <param name="Department">The final department, empty string if absent.</param>
/// <param name="Hashtag">The final hashtag, empty string if absent.</param>
/// <param name="Stats">The final statistic chips, empty for layouts that do not render them.</param>
/// <param name="Rationale">The AI's rationale for this variant, or a fallback-mode notice.</param>
/// <param name="Html">The rendered HTML document for this variant.</param>
public sealed record IconEventVariantDto(
    string Id,
    string Layout,
    string MainIcon,
    IReadOnlyList<string> SupportingIcons,
    string ColorScheme,
    string Headline,
    string Subtitle,
    string Department,
    string Hashtag,
    IReadOnlyList<IconEventStatDto> Stats,
    string Rationale,
    string Html);
