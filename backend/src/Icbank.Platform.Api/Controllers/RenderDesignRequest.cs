namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="DesignsController.RenderAsync"/>.</summary>
/// <param name="TemplateId">The template to render against.</param>
/// <param name="TitleText">The title text.</param>
/// <param name="BodyText">The body text.</param>
/// <param name="BackgroundUrl">The optional stored background-image object path.</param>
/// <param name="SelectedLogoIds">The logo ids to composite, in selection order.</param>
/// <param name="TitleFontSize">The optional title font size override.</param>
/// <param name="BodyFontSize">The optional body font size override.</param>
/// <param name="Department">The optional department badge text.</param>
/// <param name="FontFamily">The optional font family override.</param>
public sealed record RenderDesignRequest(
    int TemplateId,
    string? TitleText,
    string? BodyText,
    string? BackgroundUrl,
    IReadOnlyList<int>? SelectedLogoIds,
    double? TitleFontSize,
    double? BodyFontSize,
    string? Department,
    string? FontFamily);
