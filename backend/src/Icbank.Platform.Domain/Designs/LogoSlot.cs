namespace Icbank.Platform.Domain.Designs;

/// <summary>Typed shape for one entry of <c>design_templates.logo_slots</c> (DATA-MODEL.md section 6).</summary>
public sealed class LogoSlot
{
    /// <summary>Gets or sets the slot key.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the slot's x-coordinate.</summary>
    public double X { get; set; }

    /// <summary>Gets or sets the slot's y-coordinate.</summary>
    public double Y { get; set; }

    /// <summary>Gets or sets the legacy fixed width, kept for backward compatibility.</summary>
    public double? Width { get; set; }

    /// <summary>Gets or sets the legacy fixed height, kept for backward compatibility.</summary>
    public double? Height { get; set; }

    /// <summary>Gets or sets the maximum width for advanced templates.</summary>
    public double? MaxWidth { get; set; }

    /// <summary>Gets or sets the maximum height for advanced templates.</summary>
    public double? MaxHeight { get; set; }

    /// <summary>Gets or sets the horizontal alignment: left, center, or right.</summary>
    public string? Align { get; set; }

    /// <summary>Gets or sets the optional tint color applied to opaque pixels.</summary>
    public string? TintColor { get; set; }
}
