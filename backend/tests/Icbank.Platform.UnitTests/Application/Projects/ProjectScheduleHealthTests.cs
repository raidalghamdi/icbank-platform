using FluentAssertions;
using Icbank.Platform.Application.Projects;
using Icbank.Platform.Domain.Projects;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Projects;

/// <summary>
/// Verifies <see cref="ProjectScheduleHealth"/>: the tracking badge is a function of the dates and
/// the reported progress, so a project cannot keep claiming it is on track once its deadline has
/// passed or once actual progress has drifted behind the elapsed schedule.
/// </summary>
public sealed class ProjectScheduleHealthTests
{
    private static readonly DateTime Start = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime Due = new(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ExpectedProgressPercent_AtStart_IsZero()
        => ProjectScheduleHealth.ExpectedProgressPercent(Start, Due, Start).Should().Be(0);

    [Fact]
    public void ExpectedProgressPercent_AtDueDate_IsFull()
        => ProjectScheduleHealth.ExpectedProgressPercent(Start, Due, Due).Should().Be(100);

    [Fact]
    public void ExpectedProgressPercent_HalfwayThroughTheSchedule_IsHalf()
    {
        DateTime midpoint = Start.AddDays((Due - Start).TotalDays / 2);

        ProjectScheduleHealth.ExpectedProgressPercent(Start, Due, midpoint).Should().Be(50);
    }

    [Fact]
    public void ExpectedProgressPercent_BeforeTheStartDate_ClampsToZero()
        => ProjectScheduleHealth.ExpectedProgressPercent(Start, Due, Start.AddDays(-10)).Should().Be(0);

    [Fact]
    public void ExpectedProgressPercent_PastTheDueDate_ClampsToFull()
        => ProjectScheduleHealth.ExpectedProgressPercent(Start, Due, Due.AddDays(30)).Should().Be(100);

    [Fact]
    public void ExpectedProgressPercent_DueDateNotAfterStartDate_IsFull()
        => ProjectScheduleHealth.ExpectedProgressPercent(Start, Start, Start).Should().Be(100);

    [Fact]
    public void Evaluate_StageIsCompleted_ReturnsCompletedEvenWhenOverdue()
        => ProjectScheduleHealth.Evaluate(ProjectStage.Completed, 40, Start, Due, Due.AddDays(60))
            .Should().Be(ProjectHealth.Completed);

    [Fact]
    public void Evaluate_ProgressReachedFull_ReturnsCompleted()
        => ProjectScheduleHealth.Evaluate(ProjectStage.InProgress, 100, Start, Due, Start.AddDays(10))
            .Should().Be(ProjectHealth.Completed);

    [Fact]
    public void Evaluate_PastDueAndUnfinished_ReturnsDelayed()
        => ProjectScheduleHealth.Evaluate(ProjectStage.InProgress, 90, Start, Due, Due.AddDays(1))
            .Should().Be(ProjectHealth.Delayed);

    [Fact]
    public void Evaluate_OnHoldWithinSchedule_ReturnsAtRisk()
    {
        DateTime now = Start.AddDays(1);

        ProjectScheduleHealth.Evaluate(ProjectStage.OnHold, 99, Start, Due, now)
            .Should().Be(ProjectHealth.AtRisk);
    }

    [Fact]
    public void Evaluate_ProgressMatchesElapsedSchedule_ReturnsOnTrack()
    {
        DateTime midpoint = Start.AddDays((Due - Start).TotalDays / 2);

        ProjectScheduleHealth.Evaluate(ProjectStage.InProgress, 50, Start, Due, midpoint)
            .Should().Be(ProjectHealth.OnTrack);
    }

    [Fact]
    public void Evaluate_ProgressAheadOfSchedule_ReturnsOnTrack()
    {
        DateTime midpoint = Start.AddDays((Due - Start).TotalDays / 2);

        ProjectScheduleHealth.Evaluate(ProjectStage.InProgress, 80, Start, Due, midpoint)
            .Should().Be(ProjectHealth.OnTrack);
    }

    [Fact]
    public void Evaluate_ProgressDriftsModeratelyBehindSchedule_ReturnsAtRisk()
    {
        DateTime midpoint = Start.AddDays((Due - Start).TotalDays / 2);

        ProjectScheduleHealth.Evaluate(ProjectStage.InProgress, 40, Start, Due, midpoint)
            .Should().Be(ProjectHealth.AtRisk);
    }

    [Fact]
    public void Evaluate_ProgressDriftsFarBehindSchedule_ReturnsDelayed()
    {
        DateTime midpoint = Start.AddDays((Due - Start).TotalDays / 2);

        ProjectScheduleHealth.Evaluate(ProjectStage.InProgress, 25, Start, Due, midpoint)
            .Should().Be(ProjectHealth.Delayed);
    }

    [Fact]
    public void Evaluate_PlanningProjectThatHasBarelyStarted_ReturnsOnTrack()
        => ProjectScheduleHealth.Evaluate(ProjectStage.Planning, 0, Start, Due, Start)
            .Should().Be(ProjectHealth.OnTrack);
}
