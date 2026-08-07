namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="IconEventDesignsController.StudioAsync"/>.</summary>
/// <param name="Headline">The required headline.</param>
/// <param name="Subtitle">The optional subtitle.</param>
/// <param name="Department">The optional department.</param>
/// <param name="MainIcon">The main icon name.</param>
/// <param name="Sizes">The requested size presets.</param>
/// <param name="Layout">The layout key.</param>
/// <param name="LogoUrl">The optional logo URL.</param>
public sealed record GenerateIconEventStudioRequest(
    string Headline, string? Subtitle, string? Department, string? MainIcon, IReadOnlyList<string>? Sizes, string? Layout, string? LogoUrl);
