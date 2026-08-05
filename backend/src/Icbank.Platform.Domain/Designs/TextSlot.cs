namespace Icbank.Platform.Domain.Designs;

/// <summary>Typed shape for one entry of <c>design_templates.text_slots</c> (DATA-MODEL.md section 6).</summary>
public sealed class TextSlot
{
    /// <summary>Gets or sets the slot key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the Arabic label for the slot.</summary>
    public string LabelAr { get; set; } = string.Empty;

    /// <summary>Gets or sets the slot role, e.g. <c>title</c> or <c>body</c>.</summary>
    public string? Role { get; set; }

    /// <summary>Gets or sets the slot's x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the slot's y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the slot width.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the slot height.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the default font size.</summary>
    public double DefaultFontSize { get; set; }

    /// <summary>Gets or sets the maximum word count allowed.</summary>
    public int MaxWords { get; set; }

    /// <summary>Gets or sets the text alignment: right, center, or left.</summary>
    public string Alignment { get; set; } = "right";

    /// <summary>Gets or sets the text color.</summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional minimum font size for auto-fit.</summary>
    public double? MinFontSize { get; set; }

    /// <summary>Gets or sets the optional maximum font size for auto-fit.</summary>
    public double? MaxFontSize { get; set; }

    /// <summary>Gets or sets the optional font weight, either a keyword or numeric weight, kept as text.</summary>
    public string? FontWeight { get; set; }

    /// <summary>Gets or sets the optional line-height multiplier.</summary>
    public double? LineHeight { get; set; }
}
