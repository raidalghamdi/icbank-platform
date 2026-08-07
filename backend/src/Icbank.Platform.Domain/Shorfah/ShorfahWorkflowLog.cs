using Icbank.Platform.Domain.Common;
using Icbank.Platform.Domain.Identity;

namespace Icbank.Platform.Domain.Shorfah;

/// <summary>
/// Full audit trail of every workflow transition per section
/// (DATA-MODEL.md section 3.8 <c>shorfah_workflow_log</c>).
/// </summary>
/// <remarks>Deviation: <c>section_id</c> and <c>actor_user_id</c> were unenforced implied FKs; both are now proper, enforced foreign keys.</remarks>
public sealed class ShorfahWorkflowLog : AuditableEntity
{
    /// <summary>Gets or sets the owning section's id.</summary>
    public int SectionId { get; set; }

    /// <summary>Gets or sets the section navigation property.</summary>
    public ShorfahSection Section { get; set; } = null!;

    /// <summary>Gets or sets the id of the actor who performed the transition, if known.</summary>
    public int? ActorUserId { get; set; }

    /// <summary>Gets or sets the actor navigation property.</summary>
    public User? ActorUser { get; set; }

    /// <summary>Gets or sets the free-text action name, e.g. "submitted", "approved".</summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>Gets or sets the workflow status before the transition.</summary>
    public string? FromStatus { get; set; }

    /// <summary>Gets or sets the workflow status after the transition.</summary>
    public string? ToStatus { get; set; }

    /// <summary>Gets or sets free-text notes.</summary>
    public string? Notes { get; set; }
}
