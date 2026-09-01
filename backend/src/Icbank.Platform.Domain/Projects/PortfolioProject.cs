using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Projects;

/// <summary>
/// A project in the department's tracked portfolio. The projects page reads these rows directly
/// instead of waiting for an externally pushed report, so tracking stays available even when no
/// automation run has landed yet.
/// </summary>
public sealed class PortfolioProject : AuditableEntity
{
    /// <summary>Gets or sets the short human-readable reference shown on the card, e.g. <c>OPS-01</c>.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>Gets or sets the project name.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets the one-line description of what the project delivers.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Gets or sets the portfolio bucket the project belongs to.</summary>
    public ProjectCategory Category { get; set; }

    /// <summary>Gets or sets the lifecycle stage.</summary>
    public ProjectStage Stage { get; set; }

    /// <summary>Gets or sets the name of the person accountable for delivery.</summary>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Gets or sets the owning organisational unit.</summary>
    public string Department { get; set; } = string.Empty;

    /// <summary>Gets or sets the reported completion percentage, 0-100.</summary>
    public int ProgressPercent { get; set; }

    /// <summary>Gets or sets the number of people assigned to the project.</summary>
    public int TeamSize { get; set; }

    /// <summary>Gets or sets the UTC date delivery started.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Gets or sets the UTC date delivery is due.</summary>
    public DateTime DueDate { get; set; }

    /// <summary>Gets or sets the latest progress note shown under the bar.</summary>
    public string LatestUpdate { get; set; } = string.Empty;

    /// <summary>Gets or sets the display sort order within its category.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets a value indicating whether the project is currently tracked.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Gets the delivery checkpoints that make the progress figure auditable.</summary>
    public ICollection<ProjectMilestone> Milestones { get; } = new List<ProjectMilestone>();
}
