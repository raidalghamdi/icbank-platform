using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>Read-model DTO for a <see cref="DesignTemplate"/> row (ports the Node source's raw row shape).</summary>
/// <param name="Id">The template id.</param>
/// <param name="TemplateNameAr">The Arabic template name.</param>
/// <param name="Category">The template category.</param>
/// <param name="CanvasWidth">The canvas width in pixels.</param>
/// <param name="CanvasHeight">The canvas height in pixels.</param>
/// <param name="ThumbnailUrl">The optional thumbnail preview URL.</param>
/// <param name="PromptHint">The optional AI background-generation hint.</param>
/// <param name="CreatedAt">The creation timestamp.</param>
public sealed record DesignTemplateDto(
    int Id, string TemplateNameAr, string Category, int CanvasWidth, int CanvasHeight, string? ThumbnailUrl, string? PromptHint, DateTime CreatedAt);
