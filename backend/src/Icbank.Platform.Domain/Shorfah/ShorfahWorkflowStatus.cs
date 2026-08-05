namespace Icbank.Platform.Domain.Shorfah;

/// <summary>
/// State machine for a section's contribution workflow (DATA-MODEL.md section 5):
/// pending_contribution -> submitted -> in_review -> approved, or -> rejected.
/// </summary>
public enum ShorfahWorkflowStatus
{
    /// <summary>Awaiting the assigned contributor's submission.</summary>
    PendingContribution = 0,

    /// <summary>Submitted by the contributor, awaiting review.</summary>
    Submitted = 1,

    /// <summary>Under editorial review.</summary>
    InReview = 2,

    /// <summary>Approved by the reviewer.</summary>
    Approved = 3,

    /// <summary>Rejected by the reviewer.</summary>
    Rejected = 4,
}
