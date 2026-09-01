using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Projects;
using Icbank.Platform.Application.Projects.Queries;
using Icbank.Platform.Domain.Projects;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Projects;

/// <summary>
/// Verifies <see cref="GetProjectPortfolioQueryHandler"/>: the page receives its cards and its
/// headline figures already resolved in a single response, ordered operational-then-strategic,
/// with milestones batched onto their owning project rather than fetched per card.
/// </summary>
public sealed class GetProjectPortfolioQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 2, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly GetProjectPortfolioQueryHandler _handler;

    /// <summary>Initializes a new instance of the <see cref="GetProjectPortfolioQueryHandlerTests"/> class.</summary>
    public GetProjectPortfolioQueryHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(Now));
        _handler = new GetProjectPortfolioQueryHandler(_dbContext, _queryExecutor, _clock);
    }

    [Fact]
    public async Task Handle_NoProjects_ReturnsEmptyPortfolioWithZeroedKpis()
    {
        Arrange(Array.Empty<PortfolioProject>(), Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Projects.Should().BeEmpty();
        result.Value.Kpis.Total.Should().Be(0);
        result.Value.Kpis.AverageProgressPercent.Should().Be(0);
    }

    [Fact]
    public async Task Handle_InactiveProject_IsExcluded()
    {
        PortfolioProject hidden = MakeProject(1, "OPS-01");
        hidden.IsActive = false;
        Arrange(new[] { hidden, MakeProject(2, "OPS-02") }, Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        result.Value!.Projects.Should().ContainSingle(p => p.Code == "OPS-02");
    }

    [Fact]
    public async Task Handle_MixedCategories_OrdersOperationalBeforeStrategicThenBySortOrder()
    {
        PortfolioProject strategic = MakeProject(1, "STR-01", ProjectCategory.Strategic, sortOrder: 1);
        PortfolioProject secondOps = MakeProject(2, "OPS-02", sortOrder: 2);
        PortfolioProject firstOps = MakeProject(3, "OPS-01", sortOrder: 1);
        Arrange(new[] { strategic, secondOps, firstOps }, Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        result.Value!.Projects.Select(p => p.Code).Should().ContainInOrder("OPS-01", "OPS-02", "STR-01");
    }

    [Fact]
    public async Task Handle_Always_CountsEachCategoryIntoItsOwnKpi()
    {
        Arrange(
            new[]
            {
                MakeProject(1, "OPS-01"),
                MakeProject(2, "OPS-02"),
                MakeProject(3, "STR-01", ProjectCategory.Strategic),
            },
            Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        result.Value!.Kpis.Total.Should().Be(3);
        result.Value.Kpis.Operational.Should().Be(2);
        result.Value.Kpis.Strategic.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Always_AveragesTheReportedProgress()
    {
        Arrange(
            new[] { MakeProject(1, "OPS-01", progressPercent: 40), MakeProject(2, "OPS-02", progressPercent: 62) },
            Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        result.Value!.Kpis.AverageProgressPercent.Should().Be(51);
    }

    [Fact]
    public async Task Handle_Always_GroupsMilestonesOntoTheirOwningProjectInSortOrder()
    {
        Arrange(
            new[] { MakeProject(1, "OPS-01"), MakeProject(2, "OPS-02") },
            new[]
            {
                MakeMilestone(10, 1, "ثانية", sortOrder: 2, isCompleted: false),
                MakeMilestone(11, 1, "أولى", sortOrder: 1, isCompleted: true),
                MakeMilestone(12, 2, "لمشروع آخر", sortOrder: 1, isCompleted: true),
            });

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        PortfolioProjectDto first = result.Value!.Projects.Single(p => p.Code == "OPS-01");
        first.Milestones.Select(m => m.Title).Should().ContainInOrder("أولى", "ثانية");
        first.MilestonesTotal.Should().Be(2);
        first.MilestonesCompleted.Should().Be(1);
        result.Value.Projects.Single(p => p.Code == "OPS-02").MilestonesTotal.Should().Be(1);
    }

    [Fact]
    public async Task Handle_Always_ProjectsTheArabicLabelsAndMachineKeysForTheCard()
    {
        Arrange(new[] { MakeProject(1, "STR-01", ProjectCategory.Strategic) }, Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        PortfolioProjectDto card = result.Value!.Projects.Single();
        card.Category.Should().Be("strategic");
        card.CategoryLabel.Should().Be("استراتيجي");
        card.Stage.Should().Be("in_progress");
        card.StageLabel.Should().Be("قيد التنفيذ");
    }

    [Fact]
    public async Task Handle_OverdueProject_ReportsNegativeDaysRemainingAndDelayedHealth()
    {
        PortfolioProject overdue = MakeProject(1, "OPS-01");
        overdue.StartDate = Now.AddDays(-60);
        overdue.DueDate = Now.AddDays(-5);
        Arrange(new[] { overdue }, Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        PortfolioProjectDto card = result.Value!.Projects.Single();
        card.DaysRemaining.Should().BeNegative();
        card.Health.Should().Be("delayed");
        card.HealthLabel.Should().Be("متأخر");
        result.Value.Kpis.Delayed.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ProjectDueInsideTheNextMonth_CountsTowardsTheDueSoonKpi()
    {
        PortfolioProject dueSoon = MakeProject(1, "OPS-01");
        dueSoon.StartDate = Now.AddDays(-10);
        dueSoon.DueDate = Now.AddDays(12);
        PortfolioProject dueLater = MakeProject(2, "OPS-02");
        dueLater.StartDate = Now.AddDays(-10);
        dueLater.DueDate = Now.AddDays(120);
        Arrange(new[] { dueSoon, dueLater }, Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        result.Value!.Kpis.DueWithin30Days.Should().Be(1);
    }

    [Fact]
    public async Task Handle_CompletedProject_IsNotCountedAsDueSoon()
    {
        PortfolioProject finished = MakeProject(1, "OPS-01", progressPercent: 100);
        finished.Stage = ProjectStage.Completed;
        finished.StartDate = Now.AddDays(-20);
        finished.DueDate = Now.AddDays(10);
        Arrange(new[] { finished }, Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        result.Value!.Kpis.Completed.Should().Be(1);
        result.Value.Kpis.DueWithin30Days.Should().Be(0);
    }

    [Fact]
    public async Task Handle_Always_StampsTheResponseWithTheCurrentInstant()
    {
        Arrange(Array.Empty<PortfolioProject>(), Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        result.Value!.GeneratedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Handle_Always_ReportsTheScheduleImpliedProgressAlongsideTheReportedProgress()
    {
        PortfolioProject halfway = MakeProject(1, "OPS-01", progressPercent: 55);
        halfway.StartDate = Now.AddDays(-50);
        halfway.DueDate = Now.AddDays(50);
        Arrange(new[] { halfway }, Array.Empty<ProjectMilestone>());

        Result<ProjectPortfolioDto> result = await _handler.Handle(new GetProjectPortfolioQuery(), CancellationToken.None);

        PortfolioProjectDto card = result.Value!.Projects.Single();
        card.ProgressPercent.Should().Be(55);
        card.ExpectedProgressPercent.Should().Be(50);
        card.Health.Should().Be("on_track");
    }

    private static PortfolioProject MakeProject(
        int id,
        string code,
        ProjectCategory category = ProjectCategory.Operational,
        int progressPercent = 50,
        int sortOrder = 1)
        => new()
        {
            Id = id,
            Code = code,
            Name = "مشروع " + code,
            Description = "وصف",
            Category = category,
            Stage = ProjectStage.InProgress,
            Owner = "مسؤول",
            Department = "إدارة",
            ProgressPercent = progressPercent,
            TeamSize = 4,
            StartDate = Now.AddDays(-30),
            DueDate = Now.AddDays(30),
            LatestUpdate = "تحديث",
            SortOrder = sortOrder,
            IsActive = true,
        };

    private static ProjectMilestone MakeMilestone(int id, int projectId, string title, int sortOrder, bool isCompleted)
        => new()
        {
            Id = id,
            ProjectId = projectId,
            Title = title,
            DueDate = Now.AddDays(10),
            IsCompleted = isCompleted,
            SortOrder = sortOrder,
        };

    private void Arrange(IReadOnlyCollection<PortfolioProject> projects, IReadOnlyCollection<ProjectMilestone> milestones)
    {
        _dbContext.PortfolioProjects.Returns(projects.AsQueryable());
        _dbContext.ProjectMilestones.Returns(milestones.AsQueryable());
    }
}
