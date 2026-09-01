using Icbank.Platform.Domain.Projects;

namespace Icbank.Platform.Application.Projects;

/// <summary>
/// Shared entity-to-DTO projection for a tracked project. The portfolio query and the
/// progress-recording command both hand the browser the same card shape, so the health, schedule
/// and history logic lives here once instead of drifting between the two call sites.
/// </summary>
public static class ProjectPortfolioMapper
{
    /// <summary>The number of progress reports a card carries; older ones stay in the table but are not shipped to the browser.</summary>
    public const int MaxProgressUpdates = 10;

    /// <summary>Projects a tracked project, its checkpoints and its progress history onto the card DTO.</summary>
    /// <param name="project">The tracked project.</param>
    /// <param name="milestones">The project's checkpoints, in display order.</param>
    /// <param name="progressUpdates">The project's progress reports, in any order.</param>
    /// <param name="now">The current UTC instant, used to score the project against its schedule.</param>
    /// <returns>The card the projects page renders.</returns>
    public static PortfolioProjectDto ToDto(
        PortfolioProject project,
        IReadOnlyCollection<ProjectMilestone> milestones,
        IReadOnlyCollection<ProjectProgressUpdate> progressUpdates,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(project);
        ArgumentNullException.ThrowIfNull(milestones);
        ArgumentNullException.ThrowIfNull(progressUpdates);

        ProjectHealth health = ProjectScheduleHealth.Evaluate(project.Stage, project.ProgressPercent, project.StartDate, project.DueDate, now);

        return new PortfolioProjectDto(
            project.Id,
            project.Code,
            project.Name,
            project.Description,
            ProjectPortfolioLabels.CategoryKey(project.Category),
            ProjectPortfolioLabels.CategoryLabel(project.Category),
            ProjectPortfolioLabels.StageKey(project.Stage),
            ProjectPortfolioLabels.StageLabel(project.Stage),
            ProjectPortfolioLabels.HealthKey(health),
            ProjectPortfolioLabels.HealthLabel(health),
            project.Owner,
            project.Department,
            project.ProgressPercent,
            ProjectScheduleHealth.ExpectedProgressPercent(project.StartDate, project.DueDate, now),
            project.TeamSize,
            project.StartDate,
            project.DueDate,
            (int)Math.Ceiling((project.DueDate - now).TotalDays),
            project.LatestUpdate,
            milestones.Count(m => m.IsCompleted),
            milestones.Count,
            milestones.Select(m => new ProjectMilestoneDto(m.Id, m.Title, m.DueDate, m.IsCompleted)).ToList(),
            ToHistory(progressUpdates));
    }

    // Why: newest first and capped — the card only shows a short trail, and an unbounded history
    // on a long-running project would dominate the portfolio payload.
    private static List<ProjectProgressUpdateDto> ToHistory(IReadOnlyCollection<ProjectProgressUpdate> progressUpdates) =>
        progressUpdates
            .OrderByDescending(u => u.ReportedAt)
            .ThenByDescending(u => u.Id)
            .Take(MaxProgressUpdates)
            .Select(u => new ProjectProgressUpdateDto(u.Id, u.ProgressPercent, u.Note, u.ReportedBy, u.ReportedAt))
            .ToList();
}
