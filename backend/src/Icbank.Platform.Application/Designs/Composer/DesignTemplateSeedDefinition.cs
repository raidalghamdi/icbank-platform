using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>One template definition from a seed set (BUSINESS-RULES.md §7.1's idempotent-by-name seed source of truth).</summary>
/// <param name="TemplateNameAr">The Arabic template name, used as the idempotency key.</param>
/// <param name="Category">The template category.</param>
/// <param name="CanvasWidth">The canvas width in pixels.</param>
/// <param name="CanvasHeight">The canvas height in pixels.</param>
/// <param name="BackgroundPanelConfig">The optional background panel configuration.</param>
/// <param name="TextSlots">The text slot configuration list.</param>
/// <param name="LogoSlots">The logo slot configuration list.</param>
/// <param name="PromptHint">The optional AI background-generation hint.</param>
/// <param name="Extras">The optional extended layout configuration.</param>
public sealed record DesignTemplateSeedDefinition(
    string TemplateNameAr,
    string Category,
    int CanvasWidth,
    int CanvasHeight,
    BackgroundPanelConfig? BackgroundPanelConfig,
    List<TextSlot> TextSlots,
    List<LogoSlot> LogoSlots,
    string? PromptHint,
    TemplateExtras? Extras);
