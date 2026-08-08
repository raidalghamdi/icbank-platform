namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// The design content the studio endpoint re-renders at each requested size.
/// </summary>
/// <remarks>
/// The designer picks a style before it picks sizes, so this carries the full content of the
/// already-chosen variant back to the server. Re-sending it is what lets the size step render
/// deterministically without paying for a second extraction: the AI ran once, during generation,
/// and its output now travels with the request.
/// </remarks>
/// <param name="Headline">The headline shown on every size.</param>
/// <param name="Subtitle">The subtitle body text, if any.</param>
/// <param name="Department">The owning department; suppressed on the small and mini sizes.</param>
/// <param name="Hashtag">The campaign hashtag, if any.</param>
/// <param name="ContactEmail">The contact email chip, if any.</param>
/// <param name="ContactPhone">The contact phone chip, if any.</param>
/// <param name="Date">The event date string, if any.</param>
/// <param name="Time">The event time string, if any.</param>
/// <param name="Location">The event location string, if any.</param>
/// <param name="MainIcon">The main icon name; falls back to the library default when unknown.</param>
/// <param name="SupportingIcons">Up to three supporting icon names.</param>
/// <param name="Stats">The statistic chips, for the layouts that render them.</param>
/// <param name="Layout">The chosen layout key, for example <c>stats-hero</c>.</param>
/// <param name="LogoUrl">An override logo URL; the inline vector mark is used when omitted.</param>
public sealed record IconEventStudioContentDto(
    string Headline,
    string? Subtitle,
    string? Department,
    string? Hashtag,
    string? ContactEmail,
    string? ContactPhone,
    string? Date,
    string? Time,
    string? Location,
    string? MainIcon,
    IReadOnlyList<string>? SupportingIcons,
    IReadOnlyList<IconEventStatDto>? Stats,
    string? Layout,
    string? LogoUrl);
