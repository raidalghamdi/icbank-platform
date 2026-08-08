using Icbank.Platform.Application.Designs.IconEvent;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="IconEventDesignsController.StudioAsync"/>.</summary>
/// <param name="Headline">The required headline.</param>
/// <param name="Subtitle">The optional subtitle.</param>
/// <param name="Department">The optional department.</param>
/// <param name="Hashtag">The optional hashtag.</param>
/// <param name="ContactEmail">The optional contact email.</param>
/// <param name="ContactPhone">The optional contact phone.</param>
/// <param name="Date">The optional event date.</param>
/// <param name="Time">The optional event time.</param>
/// <param name="Location">The optional event location.</param>
/// <param name="MainIcon">The main icon name.</param>
/// <param name="SupportingIcons">The optional supporting icon names.</param>
/// <param name="Stats">The optional statistic chips.</param>
/// <param name="Layout">The layout key of the style the designer chose.</param>
/// <param name="LogoUrl">The optional logo URL.</param>
/// <param name="Sizes">The requested size preset wire values.</param>
public sealed record GenerateIconEventStudioRequest(
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
    string? LogoUrl,
    IReadOnlyList<string>? Sizes);
