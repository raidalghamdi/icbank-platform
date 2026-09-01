using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Projects;

/// <summary>
/// A delivery checkpoint on a <see cref="PortfolioProject"/>. A bare percentage tells a reader
/// nothing about what is actually done, so every project carries the checkpoints behind it.
/// </summary>
public sealed class ProjectMilestone : AuditableEntity
{
    /// <summary>Gets or sets the owning project's identifier.</summary>
    public int ProjectId { get; set; }

    /// <summary>Gets or sets the checkpoint title.</summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC date the checkpoint is due.</summary>
    public DateTime DueDate { get; set; }

    /// <summary>Gets or sets a value indicating whether the checkpoint has been delivered.</summary>
    public bool IsCompleted { get; set; }

    /// <summary>Gets or sets the display sort order within the project.</summary>
    public int SortOrder { get; set; }

    /// <summary>Gets or sets the owning project.</summary>
    public PortfolioProject? Project { get; set; }
}
