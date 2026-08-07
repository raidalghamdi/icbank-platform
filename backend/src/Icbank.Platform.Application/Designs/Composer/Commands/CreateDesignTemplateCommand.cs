using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Designs;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>Ports <c>POST /designs/templates</c> (API-SURFACE.md §17).</summary>
/// <param name="ActorUserId">The authenticated caller's user id, for audit.</param>
/// <param name="TemplateNameAr">The Arabic template name.</param>
/// <param name="Category">The template category.</param>
/// <param name="CanvasWidth">The canvas width in pixels.</param>
/// <param name="CanvasHeight">The canvas height in pixels.</param>
/// <param name="BackgroundPanelConfig">The optional background panel configuration.</param>
/// <param name="TextSlots">The text slot configuration list.</param>
/// <param name="LogoSlots">The logo slot configuration list.</param>
/// <param name="PromptHint">The optional AI background-generation hint.</param>
public sealed record CreateDesignTemplateCommand(
    int ActorUserId,
    string TemplateNameAr,
    string Category,
    int CanvasWidth,
    int CanvasHeight,
    BackgroundPanelConfig? BackgroundPanelConfig,
    List<TextSlot>? TextSlots,
    List<LogoSlot>? LogoSlots,
    string? PromptHint) : IRequest<Result<DesignTemplateDto>>;
