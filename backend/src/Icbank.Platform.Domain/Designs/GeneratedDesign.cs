using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// Record of an AI/composer-rendered output image (DATA-MODEL.md section 3.4 <c>generated_designs</c>).
/// </summary>
/// <remarks>
/// Deviation: <c>created_by</c> was an unenforced implied FK to <c>users.id</c> in the source
/// schema (DATA-MODEL.md section 4). It is now a proper, enforced foreign key via
/// <see cref="CreatedByUserId"/>/<see cref="CreatedByUser"/>.
/// </remarks>
public sealed class GeneratedDesign : AuditableEntity
{
    /// <summary>Gets or sets the source template's id, if the design was based on one.</summary>
    public int? TemplateId { get; set; }

    /// <summary>Gets or sets the template navigation property.</summary>
    public DesignTemplate? Template { get; set; }

    /// <summary>Gets or sets the rendered title text.</summary>
    public string? TitleText { get; set; }

    /// <summary>Gets or sets the rendered body text.</summary>
    public string? BodyText { get; set; }

    /// <summary>Gets or sets the background image URL used.</summary>
    public string? BackgroundImageUrl { get; set; }

    /// <summary>
    /// Gets or sets the ids of the <see cref="BrandLogo"/> rows selected for this design.
    /// Kept as a JSON-backed list (was <c>jsonb number[]</c> in source) rather than a join
    /// table, since the source models this as a simple id array with no per-selection metadata.
    /// </summary>
    public List<int> SelectedLogoIds { get; set; } = new();

    /// <summary>Gets or sets the final rendered image URL.</summary>
    public string? FinalImageUrl { get; set; }

    /// <summary>Gets or sets the free-text executive department, not FK'd to any department table in the source.</summary>
    public string? Department { get; set; }

    /// <summary>Gets or sets the id of the user who created this design.</summary>
    public int? CreatedByUserId { get; set; }

    /// <summary>Gets or sets the creating-user navigation property.</summary>
    public User? CreatedByUser { get; set; }
}
