namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// Typed shape for <c>design_templates.background_panel_config</c> (DATA-MODEL.md section 6).
/// Mapped as an EF Core owned type stored in the same table.
/// </summary>
public sealed class BackgroundPanelConfig
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

    /// <summary>Gets or sets the panel opacity (0-1).</summary>
    public double Opacity { get; set; }

    /// <summary>Gets or sets the optional corner border radius.</summary>
    public double? BorderRadius { get; set; }
}
