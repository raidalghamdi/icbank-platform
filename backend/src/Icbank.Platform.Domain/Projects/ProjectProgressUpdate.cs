using Icbank.Platform.Domain.Common;

namespace Icbank.Platform.Domain.Projects;

/// <summary>
/// One progress report logged against a <see cref="PortfolioProject"/>. A project manager reports
/// progress repeatedly over the life of a project, so the percentage on the card is only the
/// latest figure — the reports themselves are kept as rows so the trail behind that figure stays
/// auditable instead of being overwritten on every update.
/// </summary>
public sealed class ProjectProgressUpdate : AuditableEntity
{
    /// <summary>Gets or sets the owning project's identifier.</summary>
    public int ProjectId { get; set; }

    /// <summary>Gets or sets the completion percentage reported by this update, 0-100.</summary>
    public int ProgressPercent { get; set; }

    /// <summary>Gets or sets the progress note explaining what moved.</summary>
    public string Note { get; set; } = string.Empty;

    /// <summary>Gets or sets the display name of the manager who logged the update.</summary>
    public string ReportedBy { get; set; } = string.Empty;

    /// <summary>Gets or sets the UTC instant the update was logged.</summary>
    public DateTime ReportedAt { get; set; }

    /// <summary>Gets or sets the owning project.</summary>
    public PortfolioProject? Project { get; set; }
}
