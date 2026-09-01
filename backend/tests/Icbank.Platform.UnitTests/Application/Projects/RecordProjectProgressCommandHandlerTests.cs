using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Projects;
using Icbank.Platform.Application.Projects.Commands;
using Icbank.Platform.Domain.Projects;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Projects;

/// <summary>
/// Verifies <see cref="RecordProjectProgressCommandHandler"/>: a manager can log progress on the
/// same project repeatedly, each report is kept, the card's percentage follows the newest one, and
/// reporting 100% closes the project instead of leaving it open for ever.
/// </summary>
public sealed class RecordProjectProgressCommandHandlerTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly List<ProjectProgressUpdate> _appended = new();
    private readonly RecordProjectProgressCommandHandler _handler;

    /// <summary>Initializes a new instance of the <see cref="RecordProjectProgressCommandHandlerTests"/> class.</summary>
    public RecordProjectProgressCommandHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(Now));
        _dbContext.When(context => context.Add(Arg.Any<ProjectProgressUpdate>()))
            .Do(call => _appended.Add(call.Arg<ProjectProgressUpdate>()));
        _handler = new RecordProjectProgressCommandHandler(_dbContext, _queryExecutor, _clock);
    }

    [Fact]
    public async Task Handle_ValidReport_AppendsTheUpdateAndPersistsIt()
    {
        Arrange(MakeProject());

        Result<PortfolioProjectDto> result = await _handler.Handle(Command(65, "أُنجزت المرحلة الثانية"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _appended.Should().ContainSingle();
        _appended[0].ProjectId.Should().Be(1);
        _appended[0].ProgressPercent.Should().Be(65);
        _appended[0].Note.Should().Be("أُنجزت المرحلة الثانية");
        _appended[0].ReportedBy.Should().Be("نورة القحطاني");
        _appended[0].ReportedAt.Should().Be(Now);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidReport_MovesTheProjectPercentageAndLatestNote()
    {
        PortfolioProject project = MakeProject(progressPercent: 20);
        Arrange(project);

        Result<PortfolioProjectDto> result = await _handler.Handle(Command(65, "أُنجزت المرحلة الثانية"), CancellationToken.None);

        project.ProgressPercent.Should().Be(65);
        project.LatestUpdate.Should().Be("أُنجزت المرحلة الثانية");
        result.Value!.ProgressPercent.Should().Be(65);
        result.Value.LatestUpdate.Should().Be("أُنجزت المرحلة الثانية");
    }

    [Fact]
    public async Task Handle_SecondReportOnTheSameProject_KeepsTheEarlierOneInTheHistory()
    {
        PortfolioProject project = MakeProject();
        Arrange(project, MakeUpdate(10, 30, "المرحلة الأولى", Now.AddDays(-7)));

        Result<PortfolioProjectDto> result = await _handler.Handle(Command(65, "المرحلة الثانية"), CancellationToken.None);

        result.Value!.ProgressUpdates.Should().HaveCount(2);
        result.Value.ProgressUpdates[0].Note.Should().Be("المرحلة الثانية");
        result.Value.ProgressUpdates[1].Note.Should().Be("المرحلة الأولى");
    }

    [Fact]
    public async Task Handle_LongHistory_ReturnsOnlyTheTenMostRecentReports()
    {
        PortfolioProject project = MakeProject();
        ProjectProgressUpdate[] history = Enumerable.Range(1, 14)
            .Select(index => MakeUpdate(index, index, $"تحديث {index}", Now.AddDays(-index)))
            .ToArray();
        Arrange(project, history);

        Result<PortfolioProjectDto> result = await _handler.Handle(Command(65, "الأحدث"), CancellationToken.None);

        result.Value!.ProgressUpdates.Should().HaveCount(ProjectPortfolioMapper.MaxProgressUpdates);
        result.Value.ProgressUpdates[0].Note.Should().Be("الأحدث");
        result.Value.ProgressUpdates.Should().BeInDescendingOrder(update => update.ReportedAt);
    }

    [Fact]
    public async Task Handle_FullProgress_ClosesTheProject()
    {
        PortfolioProject project = MakeProject();
        Arrange(project);

        Result<PortfolioProjectDto> result = await _handler.Handle(Command(100, "اكتمل التسليم"), CancellationToken.None);

        project.Stage.Should().Be(ProjectStage.Completed);
        result.Value!.Stage.Should().Be("completed");
        result.Value.Health.Should().Be("completed");
    }

    [Fact]
    public async Task Handle_PartialProgress_LeavesTheStageAlone()
    {
        PortfolioProject project = MakeProject();
        Arrange(project);

        await _handler.Handle(Command(99, "شارفنا على الانتهاء"), CancellationToken.None);

        project.Stage.Should().Be(ProjectStage.InProgress);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(101)]
    public async Task Handle_PercentageOutsideZeroToHundred_FailsWithoutTouchingTheProject(int progressPercent)
    {
        PortfolioProject project = MakeProject(progressPercent: 20);
        Arrange(project);

        Result<PortfolioProjectDto> result = await _handler.Handle(Command(progressPercent, "ملاحظة"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(RecordProjectProgressCommand.ProgressOutOfRangeError);
        project.ProgressPercent.Should().Be(20);
        _appended.Should().BeEmpty();
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_EmptyNote_Fails(string note)
    {
        Arrange(MakeProject());

        Result<PortfolioProjectDto> result = await _handler.Handle(Command(40, note), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(RecordProjectProgressCommand.EmptyNoteError);
        _appended.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_UnknownProject_FailsAsNotFound()
    {
        Arrange();

        Result<PortfolioProjectDto> result = await _handler.Handle(Command(40, "ملاحظة"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(RecordProjectProgressCommand.ProjectNotFoundError);
    }

    [Fact]
    public async Task Handle_UntrackedProject_FailsAsNotFound()
    {
        PortfolioProject retired = MakeProject();
        retired.IsActive = false;
        Arrange(retired);

        Result<PortfolioProjectDto> result = await _handler.Handle(Command(40, "ملاحظة"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(RecordProjectProgressCommand.ProjectNotFoundError);
        _appended.Should().BeEmpty();
    }

    private static RecordProjectProgressCommand Command(int progressPercent, string note)
        => new(1, progressPercent, note, "نورة القحطاني");

    private static PortfolioProject MakeProject(int progressPercent = 40)
        => new()
        {
            Id = 1,
            Code = "OPS-01",
            Name = "مشروع",
            Description = "وصف",
            Category = ProjectCategory.Operational,
            Stage = ProjectStage.InProgress,
            Owner = "مسؤول",
            Department = "إدارة",
            ProgressPercent = progressPercent,
            TeamSize = 4,
            StartDate = Now.AddDays(-30),
            DueDate = Now.AddDays(30),
            LatestUpdate = "تحديث سابق",
            SortOrder = 1,
            IsActive = true,
        };

    private static ProjectProgressUpdate MakeUpdate(int id, int progressPercent, string note, DateTime reportedAt)
        => new()
        {
            Id = id,
            ProjectId = 1,
            ProgressPercent = progressPercent,
            Note = note,
            ReportedBy = "مسؤول",
            ReportedAt = reportedAt,
        };

    private void Arrange(PortfolioProject? project = null, params ProjectProgressUpdate[] history)
    {
        PortfolioProject[] projects = project is null ? Array.Empty<PortfolioProject>() : new[] { project };
        _dbContext.PortfolioProjects.Returns(projects.AsQueryable());
        _dbContext.ProjectMilestones.Returns(Array.Empty<ProjectMilestone>().AsQueryable());
        _dbContext.ProjectProgressUpdates.Returns(history.AsQueryable());
    }
}
