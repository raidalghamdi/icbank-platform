using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Shorfah;

/// <summary>Photo/file attached to a section (DATA-MODEL.md section 3.8 <c>shorfah_section_media</c>).</summary>
/// <remarks>Deviation: <c>section_id</c> was an unenforced implied FK; it is now a proper, enforced foreign key.</remarks>
public sealed class ShorfahSectionMedia : AuditableEntity
{
    /// <summary>Gets or sets the owning section's id.</summary>
    public int SectionId { get; set; }

    /// <summary>Gets or sets the section navigation property.</summary>
    public ShorfahSection Section { get; set; } = null!;

    /// <summary>Gets or sets the media URL.</summary>
    public string MediaUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the media kind.</summary>
    public ShorfahMediaType MediaType { get; set; }

    /// <summary>Gets or sets an optional Arabic caption.</summary>
    public string? CaptionAr { get; set; }

    /// <summary>Gets or sets the display sort order.</summary>
    public int? DisplayOrder { get; set; }
}
