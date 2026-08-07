using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.InternationalDays;

/// <summary>
/// Recorded campaign activation (by Saudi/regional entities) for a given day and year
/// (DATA-MODEL.md section 3.6 <c>day_activations</c>).
/// </summary>
public sealed class DayActivation : AuditableEntity
{
    /// <summary>Gets or sets the owning day's id.</summary>
    public int DayId { get; set; }

    /// <summary>Gets or sets the day navigation property.</summary>
    public InternationalDay Day { get; set; } = null!;

    /// <summary>Gets or sets the campaign year, if known.</summary>
    public int? Year { get; set; }

    /// <summary>Gets or sets the entity that ran the activation.</summary>
    public string? EntityName { get; set; }

    /// <summary>Gets or sets the entity type, free text (e.g. government, private, international).</summary>
    public string? EntityType { get; set; }

    /// <summary>Gets or sets the activation type, free text (e.g. campaign, event, post, infographic).</summary>
    public string? ActivationType { get; set; }

    /// <summary>Gets or sets the platform the activation ran on.</summary>
    public string? Platform { get; set; }

    /// <summary>Gets or sets a free-text description.</summary>
    public string? Description { get; set; }

    /// <summary>Gets or sets the attached media URL.</summary>
    public string? MediaUrl { get; set; }

    /// <summary>Gets or sets the source URL used to verify the activation.</summary>
    public string? SourceUrl { get; set; }

    /// <summary>Gets or sets the country the activation took place in.</summary>
    public string? Country { get; set; }

    /// <summary>Gets or sets a value indicating whether the activation was verified via a source URL.</summary>
    public bool Verified { get; set; }
}
