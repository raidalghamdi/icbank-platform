namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// Typed shape for <c>design_templates.extras</c> -- the most complex JSON shape in the schema
/// (DATA-MODEL.md section 6). Mapped as an EF Core owned type serialized to a single JSON column.
/// </summary>
public sealed class TemplateExtras
{
    /// <summary>Gets or sets the layout kind: social, presentation-paragraphs, or presentation-icons-2x2.</summary>
    public string LayoutKind { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional gradient header configuration.</summary>
    public GradientHeader? GradientHeader { get; set; }

    /// <summary>Gets or sets the optional department badge configuration.</summary>
    public DepartmentBadge? DepartmentBadge { get; set; }

    /// <summary>Gets or sets the optional image placeholder configuration.</summary>
    public ImagePlaceholder? ImagePlaceholder { get; set; }

    /// <summary>Gets or sets the optional vertical separator configuration.</summary>
    public VerticalSeparator? VerticalSeparator { get; set; }

    /// <summary>Gets or sets the optional content panel configuration.</summary>
    public ContentPanel? ContentPanel { get; set; }

    /// <summary>Gets or sets the optional icon slot list.</summary>
    public List<IconSlot> IconSlots { get; set; } = new();

    /// <summary>Gets or sets the optional sub-heading configuration.</summary>
    public SubHeading? SubHeading { get; set; }
}

/// <summary>Gradient header band configuration nested under <see cref="TemplateExtras"/>.</summary>
public sealed class GradientHeader
{
    /// <summary>Gets or sets the header height as a percentage of the canvas.</summary>
    public double HeightPct { get; set; }

    /// <summary>Gets or sets the gradient start color.</summary>
    public string ColorStart { get; set; } = string.Empty;

    /// <summary>Gets or sets the gradient end color.</summary>
    public string ColorEnd { get; set; } = string.Empty;

    /// <summary>Gets or sets the gradient direction: horizontal, vertical, or diagonal.</summary>
    public string? Direction { get; set; }
}

/// <summary>Department badge configuration nested under <see cref="TemplateExtras"/>.</summary>
public sealed class DepartmentBadge
{
    /// <summary>Gets or sets the badge's x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the badge's y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the badge width.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the badge height.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the badge background color.</summary>
    public string BgColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the badge text color.</summary>
    public string TextColor { get; set; } = string.Empty;

    /// <summary>Gets or sets the badge font size.</summary>
    public double FontSize { get; set; }

    /// <summary>Gets or sets the optional badge corner radius.</summary>
    public double? BorderRadius { get; set; }

    /// <summary>Gets or sets the optional text alignment: right, center, or left.</summary>
    public string? TextAlign { get; set; }
}

/// <summary>Image placeholder configuration nested under <see cref="TemplateExtras"/>.</summary>
public sealed class ImagePlaceholder
{
    /// <summary>Gets or sets the placeholder's x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the placeholder's y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the placeholder width.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the placeholder height.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the optional placeholder label text.</summary>
    public string? Label { get; set; }

    /// <summary>Gets or sets the optional placeholder background color.</summary>
    public string? BgColor { get; set; }

    /// <summary>Gets or sets the optional label text color.</summary>
    public string? LabelColor { get; set; }

    /// <summary>Gets or sets the optional label font size.</summary>
    public double? LabelFontSize { get; set; }

    /// <summary>Gets or sets the optional corner radius.</summary>
    public double? BorderRadius { get; set; }
}

/// <summary>One icon-grid entry nested under <see cref="TemplateExtras"/>.</summary>
public sealed class IconSlot
{
    /// <summary>Gets or sets the icon's x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the icon's y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the icon size.</summary>
    public double Size { get; set; }

    /// <summary>Gets or sets the Lucide icon name.</summary>
    public string LucideName { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional icon color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the optional icon stroke width.</summary>
    public double? StrokeWidth { get; set; }

    /// <summary>Gets or sets the optional title text shown next to the icon.</summary>
    public string? TitleText { get; set; }

    /// <summary>Gets or sets the optional title text color.</summary>
    public string? TitleColor { get; set; }

    /// <summary>Gets or sets the optional title font size.</summary>
    public double? TitleFontSize { get; set; }

    /// <summary>Gets or sets the optional body text shown next to the icon.</summary>
    public string? BodyText { get; set; }

    /// <summary>Gets or sets the optional body text color.</summary>
    public string? BodyColor { get; set; }

    /// <summary>Gets or sets the optional body font size.</summary>
    public double? BodyFontSize { get; set; }

    /// <summary>Gets or sets the optional text column width.</summary>
    public double? TextWidth { get; set; }

    /// <summary>Gets or sets the optional text alignment: right, center, or left.</summary>
    public string? TextAlign { get; set; }
}

/// <summary>Vertical separator line configuration nested under <see cref="TemplateExtras"/>.</summary>
public sealed class VerticalSeparator
{
    /// <summary>Gets or sets the separator's x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the separator's y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the separator width.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the separator height.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the optional separator color.</summary>
    public string? Color { get; set; }
}

/// <summary>Content panel configuration nested under <see cref="TemplateExtras"/>.</summary>
public sealed class ContentPanel
{
    /// <summary>Gets or sets the panel's x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the panel's y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the panel width.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the panel height.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the panel fill color.</summary>
    public string Color { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional panel opacity (0-1).</summary>
    public double? Opacity { get; set; }

    /// <summary>Gets or sets the optional corner radius.</summary>
    public double? BorderRadius { get; set; }
}

/// <summary>Sub-heading text block configuration nested under <see cref="TemplateExtras"/>.</summary>
public sealed class SubHeading
{
    /// <summary>Gets or sets the sub-heading's x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the sub-heading's y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the sub-heading width.</summary>
    public double Width { get; set; }

    /// <summary>Gets or sets the sub-heading height.</summary>
    public double Height { get; set; }

    /// <summary>Gets or sets the optional text color.</summary>
    public string? Color { get; set; }

    /// <summary>Gets or sets the optional font size.</summary>
    public double? FontSize { get; set; }

    /// <summary>Gets or sets the optional font weight, either a keyword or numeric weight, kept as text.</summary>
    public string? FontWeight { get; set; }

    /// <summary>Gets or sets the optional text alignment: right, center, or left.</summary>
    public string? TextAlign { get; set; }

    /// <summary>Gets or sets the optional literal text content.</summary>
    public string? Text { get; set; }
}
