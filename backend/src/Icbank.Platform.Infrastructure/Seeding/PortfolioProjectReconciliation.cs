using Icbank.Platform.Domain.Projects;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// The change set <see cref="PortfolioProjectReconciler"/> produced for the tracked portfolio.
/// Returned as data rather than applied inline so the reconciliation rules can be exercised
/// without a database.
/// </summary>
/// <param name="Added">Projects present in the catalogue but missing from the table.</param>
/// <param name="Updated">Tracked projects whose fields and checkpoints were refreshed from the catalogue.</param>
/// <param name="Removed">Tracked projects whose code is no longer in the catalogue.</param>
/// <param name="RemovedMilestones">Checkpoints to delete: those of removed projects plus the replaced sets of updated ones.</param>
/// <param name="RemovedProgressUpdates">Progress reports to delete, belonging to removed projects.</param>
internal sealed record PortfolioProjectReconciliation(
    IReadOnlyList<PortfolioProject> Added,
    IReadOnlyList<PortfolioProject> Updated,
    IReadOnlyList<PortfolioProject> Removed,
    IReadOnlyList<ProjectMilestone> RemovedMilestones,
    IReadOnlyList<ProjectProgressUpdate> RemovedProgressUpdates)
{
    /// <summary>Gets a value indicating whether the reconciliation would change anything at all.</summary>
    internal bool HasChanges => Added.Count > 0 || Updated.Count > 0 || Removed.Count > 0;
}
