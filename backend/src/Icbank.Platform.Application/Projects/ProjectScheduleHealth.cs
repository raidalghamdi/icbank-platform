using Icbank.Platform.Domain.Projects;

namespace Icbank.Platform.Application.Projects;

/// <summary>
/// Derives where a project stands against its own schedule. Storing a health badge would let a
/// project keep claiming it is on track long after its deadline passed, so the badge is computed
/// from the dates and the reported progress every time the portfolio is read.
/// </summary>
public static class ProjectScheduleHealth
{
    private const int FullPercent = 100;
    private const int AtRiskDriftPercent = 8;
    private const int DelayedDriftPercent = 20;

    /// <summary>Calculates the completion percentage the elapsed schedule implies.</summary>
    /// <param name="startDate">The UTC start date.</param>
    /// <param name="dueDate">The UTC due date.</param>
    /// <param name="now">The current UTC instant.</param>
    /// <returns>A value between 0 and 100.</returns>
    public static int ExpectedProgressPercent(DateTime startDate, DateTime dueDate, DateTime now)
    {
        var totalDays = (dueDate - startDate).TotalDays;
        if (totalDays <= 0)
        {
            return FullPercent;
        }

        var elapsed = (now - startDate).TotalDays / totalDays * FullPercent;
        return Math.Clamp((int)Math.Round(elapsed), 0, FullPercent);
    }

    /// <summary>Derives the tracking signal for a project.</summary>
    /// <param name="stage">The lifecycle stage.</param>
    /// <param name="progressPercent">The reported completion percentage.</param>
    /// <param name="startDate">The UTC start date.</param>
    /// <param name="dueDate">The UTC due date.</param>
    /// <param name="now">The current UTC instant.</param>
    /// <returns>The signal to show next to the project.</returns>
    public static ProjectHealth Evaluate(ProjectStage stage, int progressPercent, DateTime startDate, DateTime dueDate, DateTime now)
    {
        if (stage == ProjectStage.Completed || progressPercent >= FullPercent)
        {
            return ProjectHealth.Completed;
        }

        if (now > dueDate)
        {
            return ProjectHealth.Delayed;
        }

        if (stage == ProjectStage.OnHold)
        {
            return ProjectHealth.AtRisk;
        }

        var drift = ExpectedProgressPercent(startDate, dueDate, now) - progressPercent;
        return drift >= DelayedDriftPercent
            ? ProjectHealth.Delayed
            : drift >= AtRiskDriftPercent ? ProjectHealth.AtRisk : ProjectHealth.OnTrack;
    }
}
