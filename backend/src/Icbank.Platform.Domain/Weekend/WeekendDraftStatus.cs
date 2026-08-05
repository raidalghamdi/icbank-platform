namespace Icbank.Platform.Domain.Weekend;

/// <summary>
/// State machine for a weekend content draft (DATA-MODEL.md section 5):
/// pending_review -> approved -> published, or -> rejected.
/// </summary>
public enum WeekendDraftStatus
{
    /// <summary>Awaiting editorial review.</summary>
    PendingReview = 0,

    /// <summary>Approved by an editor.</summary>
    Approved = 1,

    /// <summary>Published to end users.</summary>
    Published = 2,

    /// <summary>Rejected by an editor.</summary>
    Rejected = 3,
}
