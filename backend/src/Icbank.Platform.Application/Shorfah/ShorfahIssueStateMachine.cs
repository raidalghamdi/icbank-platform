using Icbank.Platform.Domain.Shorfah;

namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// The issue lifecycle state machine (BUSINESS-RULES.md §1.1): <c>collecting -> in_review ->
/// published</c>, with <c>collect</c> being idempotent (a no-op status-wise once published) and
/// no reverse (unpublish) transition existing anywhere in the API. The Node source enforced these
/// rules ad hoc, inline, per endpoint; this port centralizes the same rules in one place so every
/// caller (the three transition endpoints, plus <c>PATCH /shorfah/issues/:id</c> when it targets
/// <c>Status</c>) enforces them identically and illegal transitions fail with a clear error
/// instead of silently succeeding (task requirement: "enforce illegal transitions with a clear
/// error rather than silently allowing them").
/// </summary>
public static class ShorfahIssueStateMachine
{
    /// <summary>
    /// Validates a <c>start-review</c> transition (BUSINESS-RULES.md §1.1: blocked only if
    /// already <c>published</c>; notably permits <c>collecting -> in_review</c> even with zero
    /// submitted sections, matching the Node source's documented gap verbatim -- this is not a
    /// bug this port silently fixes, since doing so would be a scope-creep behaviour change).
    /// </summary>
    /// <param name="current">The issue's current status.</param>
    /// <returns><c>true</c> if the transition is legal.</returns>
    public static bool CanStartReview(ShorfahIssueStatus current) => current != ShorfahIssueStatus.Published;

    /// <summary>
    /// Validates a direct <c>PATCH</c>-driven status assignment. Unlike the dedicated transition
    /// endpoints (which each enforce one specific rule), an arbitrary <c>PATCH</c> could otherwise
    /// jump to any status -- this port requires the target to be reachable via a legal single
    /// step from the current status (forward progression only, matching the fact that no reverse
    /// transition exists anywhere in the source system).
    /// </summary>
    /// <param name="current">The issue's current status.</param>
    /// <param name="target">The requested status.</param>
    /// <returns><c>true</c> if the transition is legal.</returns>
    public static bool CanTransitionTo(ShorfahIssueStatus current, ShorfahIssueStatus target)
    {
        if (current == target)
        {
            return true;
        }

        return (current, target) switch
        {
            (ShorfahIssueStatus.Collecting, ShorfahIssueStatus.InReview) => true,
            (ShorfahIssueStatus.InReview, ShorfahIssueStatus.Published) => true,
            _ => false,
        };
    }
}
