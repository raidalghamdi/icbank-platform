using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// Reusable branded poster/slide template with text/logo slot configuration
/// (DATA-MODEL.md section 3.4 <c>design_templates</c>).
/// </summary>
public sealed class DesignTemplate : AuditableEntity
{
    /// <summary>Gets or sets the Arabic template name.</summary>
    public string TemplateNameAr { get; set; } = string.Empty;

    /// <summary>Gets or sets the template category.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>Gets or sets the canvas width in pixels.</summary>
    public int CanvasWidth { get; set; } = 1920;

    /// <summary>Gets or sets the canvas height in pixels.</summary>
    public int CanvasHeight { get; set; } = 1080;

    /// <summary>Gets or sets the optional background panel configuration.</summary>
    public BackgroundPanelConfig? BackgroundPanelConfig { get; set; }

    /// <summary>Gets or sets the text slot configuration list.</summary>
    public List<TextSlot> TextSlots { get; set; } = new();

    /// <summary>Gets or sets the logo slot configuration list.</summary>
    public List<LogoSlot> LogoSlots { get; set; } = new();

    /// <summary>Gets or sets the thumbnail preview URL.</summary>
    public string? ThumbnailUrl { get; set; }

    /// <summary>Gets or sets an optional hint appended to the AI background prompt.</summary>
    public string? PromptHint { get; set; }

    /// <summary>Gets or sets the extended configuration for presentation/v2 social templates.</summary>
    public TemplateExtras? Extras { get; set; }

    /// <summary>Gets the designs generated from this template.</summary>
    public ICollection<GeneratedDesign> GeneratedDesigns { get; init; } = new List<GeneratedDesign>();
}
