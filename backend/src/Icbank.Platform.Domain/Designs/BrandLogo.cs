using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Designs;

/// <summary>Uploaded brand logo asset available to the composer (DATA-MODEL.md section 3.4 <c>brand_logos</c>).</summary>
public sealed class BrandLogo : AuditableEntity
{
    /// <summary>Gets or sets the logo's display name.</summary>
    public string LogoName { get; set; } = string.Empty;

    /// <summary>Gets or sets the file URL.</summary>
    public string FileUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the logo has a transparent background.</summary>
    public bool Transparent { get; set; }

    /// <summary>Gets or sets the default render width, if set.</summary>
    public int? DefaultWidth { get; set; }
}
