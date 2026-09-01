using Icbank.Platform.Domain.Projects;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>
/// Makes the tracked portfolio match <see cref="PortfolioProjectSeedCatalog"/> exactly. Inserting
/// only the missing codes — what the seeder used to do — left retired projects on the page for
/// ever and let a renamed project keep its old title in every environment that had already been
/// seeded, so the catalogue is treated as authoritative in both directions: codes it no longer
/// lists are deleted with their children, codes it lists are overwritten field by field.
/// </summary>
internal static class PortfolioProjectReconciler
{
    /// <summary>Builds the change set that brings <paramref name="tracked"/> in line with the catalogue.</summary>
    /// <param name="tracked">Every project currently in the table, with its checkpoints and progress reports loaded.</param>
    /// <param name="seededAt">The instant relative catalogue dates are resolved against for brand-new rows.</param>
    /// <returns>The reconciliation to apply.</returns>
    internal static PortfolioProjectReconciliation Reconcile(IReadOnlyCollection<PortfolioProject> tracked, DateTime seededAt)
    {
        (Dictionary<string, PortfolioProject> survivors, List<PortfolioProject> removed) = Partition(tracked);

        var added = new List<PortfolioProject>();
        var updated = new List<PortfolioProject>();
        var removedMilestones = removed.SelectMany(project => project.Milestones).ToList();

        foreach (PortfolioProjectSeedRow row in PortfolioProjectSeedCatalog.Rows)
        {
            if (!survivors.TryGetValue(row.Code, out PortfolioProject? existing))
            {
                added.Add(Build(row, seededAt));
                continue;
            }

            // Why: relative dates are anchored to the row's own creation instant, not to "now", so
            // a restart does not silently shift every schedule by a day.
            DateTime anchor = existing.CreatedAt == default ? seededAt : existing.CreatedAt;
            List<ProjectMilestone> replaced = RefreshMilestones(existing, row, anchor);
            var changed = Apply(existing, row, anchor) || replaced.Count > 0;
            removedMilestones.AddRange(replaced);
            if (changed)
            {
                updated.Add(existing);
            }
        }

        return new PortfolioProjectReconciliation(
            added,
            updated,
            removed,
            removedMilestones,
            removed.SelectMany(project => project.ProgressUpdates).ToList());
    }

    /// <summary>Creates a brand-new tracked project from a catalogue row.</summary>
    /// <param name="row">The catalogue row.</param>
    /// <param name="seededAt">The instant relative dates are resolved against.</param>
    /// <returns>The project, with its checkpoints attached.</returns>
    internal static PortfolioProject Build(PortfolioProjectSeedRow row, DateTime seededAt)
    {
        var project = new PortfolioProject { Code = row.Code, CreatedBy = "seeder" };
        Apply(project, row, seededAt);
        AddMilestones(project, row, seededAt);
        return project;
    }

    // Splits the table into the row that owns each catalogue code and everything else. A duplicate
    // row claiming a catalogue code is as stale as an unknown code: only one row can be the
    // project the catalogue describes.
    private static (Dictionary<string, PortfolioProject> Survivors, List<PortfolioProject> Removed) Partition(
        IReadOnlyCollection<PortfolioProject> tracked)
    {
        var catalogCodes = PortfolioProjectSeedCatalog.Rows.Select(row => row.Code).ToHashSet(StringComparer.Ordinal);
        var survivors = new Dictionary<string, PortfolioProject>(StringComparer.Ordinal);
        var removed = new List<PortfolioProject>();

        foreach (PortfolioProject project in tracked)
        {
            if (!catalogCodes.Contains(project.Code) || !survivors.TryAdd(project.Code, project))
            {
                removed.Add(project);
            }
        }

        return (survivors, removed);
    }

    // Why: every catalogue-owned field is overwritten, so a project that was renamed or re-scoped
    // in the catalogue stops showing its old wording on the page. Returns whether anything moved,
    // which keeps a no-op run from logging a reconciliation that did not happen.
    private static bool Apply(PortfolioProject project, PortfolioProjectSeedRow row, DateTime anchor)
    {
        DateTime startDate = anchor.AddDays(row.StartOffsetDays).Date;
        DateTime dueDate = anchor.AddDays(row.DueOffsetDays).Date;
        var changed = !string.Equals(project.Name, row.Name, StringComparison.Ordinal)
            || !string.Equals(project.Description, row.Description, StringComparison.Ordinal)
            || !string.Equals(project.Owner, row.Owner, StringComparison.Ordinal)
            || !string.Equals(project.Department, row.Department, StringComparison.Ordinal)
            || !string.Equals(project.LatestUpdate, row.LatestUpdate, StringComparison.Ordinal)
            || project.Category != row.Category
            || project.Stage != row.Stage
            || project.ProgressPercent != row.ProgressPercent
            || project.TeamSize != row.TeamSize
            || project.SortOrder != row.SortOrder
            || project.StartDate != startDate
            || project.DueDate != dueDate
            || !project.IsActive;

        project.Name = row.Name;
        project.Description = row.Description;
        project.Category = row.Category;
        project.Stage = row.Stage;
        project.Owner = row.Owner;
        project.Department = row.Department;
        project.ProgressPercent = row.ProgressPercent;
        project.TeamSize = row.TeamSize;
        project.StartDate = startDate;
        project.DueDate = dueDate;
        project.LatestUpdate = row.LatestUpdate;
        project.SortOrder = row.SortOrder;
        project.IsActive = true;
        return changed;
    }

    // Replaces the checkpoint set only when it actually differs from the catalogue: rewriting an
    // identical set on every restart would churn the rows' identities for no reason.
    private static List<ProjectMilestone> RefreshMilestones(PortfolioProject project, PortfolioProjectSeedRow row, DateTime anchor)
    {
        var current = project.Milestones.OrderBy(milestone => milestone.SortOrder).ToList();
        if (MilestonesMatch(current, row, anchor))
        {
            return new List<ProjectMilestone>();
        }

        project.Milestones.Clear();
        AddMilestones(project, row, anchor);
        return current;
    }

    private static bool MilestonesMatch(List<ProjectMilestone> current, PortfolioProjectSeedRow row, DateTime anchor)
    {
        if (current.Count != row.Milestones.Count)
        {
            return false;
        }

        return !current.Where((milestone, index) =>
            !string.Equals(milestone.Title, row.Milestones[index].Title, StringComparison.Ordinal)
            || milestone.DueDate != anchor.AddDays(row.Milestones[index].DueOffsetDays).Date
            || milestone.IsCompleted != row.Milestones[index].IsCompleted).Any();
    }

    private static void AddMilestones(PortfolioProject project, PortfolioProjectSeedRow row, DateTime anchor)
    {
        var order = 1;
        foreach (PortfolioMilestoneSeedRow milestone in row.Milestones)
        {
            project.Milestones.Add(new ProjectMilestone
            {
                Title = milestone.Title,
                DueDate = anchor.AddDays(milestone.DueOffsetDays).Date,
                IsCompleted = milestone.IsCompleted,
                SortOrder = order++,
                CreatedBy = "seeder",
            });
        }
    }
}
