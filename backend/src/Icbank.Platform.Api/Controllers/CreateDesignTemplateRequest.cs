using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="DesignsController.CreateTemplateAsync"/>.</summary>
/// <param name="TemplateNameAr">The Arabic template name.</param>
/// <param name="Category">The template category.</param>
/// <param name="CanvasWidth">The canvas width in pixels.</param>
/// <param name="CanvasHeight">The canvas height in pixels.</param>
/// <param name="BackgroundPanelConfig">The optional background panel configuration.</param>
/// <param name="TextSlots">The text slot configuration list.</param>
/// <param name="LogoSlots">The logo slot configuration list.</param>
/// <param name="PromptHint">The optional AI background-generation hint.</param>
public sealed record CreateDesignTemplateRequest(
    string TemplateNameAr,
    string Category,
    int CanvasWidth,
    int CanvasHeight,
    BackgroundPanelConfig? BackgroundPanelConfig,
    List<TextSlot>? TextSlots,
    List<LogoSlot>? LogoSlots,
    string? PromptHint);
