using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// Uploaded font file for the composer (DATA-MODEL.md section 3.4 <c>brand_fonts</c>). Only one
/// row is expected to have <see cref="IsDefault"/> set; DATA-MODEL.md flags this ("DATA-01") as
/// enforced only by application logic in the source system. The EF configuration adds a
/// filtered unique index to enforce it at the database level going forward.
/// </summary>
public sealed class BrandFont : AuditableEntity
{
    /// <summary>Gets or sets the font's display name.</summary>
    public string FontName { get; set; } = string.Empty;

    /// <summary>Gets or sets the font file URL.</summary>
    public string FontFileUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether this is the default font.</summary>
    public bool IsDefault { get; set; }
}
