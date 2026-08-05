using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.MediaMonitoring;
using Icbank.Platform.UnitTests.Application;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Verifies <see cref="SearchFinalMediaReportsCommandHandler"/> full vs info mode split, and that every query is logged regardless of mode.</summary>
public sealed class SearchFinalMediaReportsCommandHandlerTests
{
    private const int ActorUserId = 8;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IReportArchiveQaEngine _qaEngine = Substitute.For<IReportArchiveQaEngine>();
    private readonly SearchFinalMediaReportsCommandHandler _handler;

    public SearchFinalMediaReportsCommandHandlerTests()
    {
        _handler = new SearchFinalMediaReportsCommandHandler(_dbContext, _queryExecutor, _qaEngine);
    }

    [Fact]
    public async Task Handle_FullMode_ReturnsMatchedReportsAndNeverCallsQaEngine()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        report.Title = "تقرير المنافسة";
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());

        Result<SearchFinalMediaReportsResultDto> result = await _handler.Handle(
            new SearchFinalMediaReportsCommand(ActorUserId, "full", "المنافسة", null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Mode.Should().Be("full");
        result.Value.Reports.Should().ContainSingle();
        result.Value.Answer.Should().BeNull();
        await _qaEngine.DidNotReceive().AnswerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InfoMode_CallsQaEngineAndReturnsAnswer()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        report.Title = "تقرير المنافسة";
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());
        _qaEngine.AnswerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns("إجابة");

        Result<SearchFinalMediaReportsResultDto> result = await _handler.Handle(
            new SearchFinalMediaReportsCommand(ActorUserId, "info", "ما آخر التطورات؟", null), CancellationToken.None);

        result.Value!.Mode.Should().Be("info");
        result.Value.Answer.Should().Be("إجابة");
        result.Value.Reports.Should().BeNull();
    }

    [Fact]
    public async Task Handle_NoMatches_ReturnsEmptyReportsListInFullMode()
    {
        _dbContext.FinalMediaReports.Returns(Array.Empty<FinalMediaReport>().AsQueryable());

        Result<SearchFinalMediaReportsResultDto> result = await _handler.Handle(
            new SearchFinalMediaReportsCommand(ActorUserId, "full", "لا يوجد تطابق", null), CancellationToken.None);

        result.Value!.Reports.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AnyMode_LogsQueryToArchive()
    {
        _dbContext.FinalMediaReports.Returns(Array.Empty<FinalMediaReport>().AsQueryable());

        await _handler.Handle(new SearchFinalMediaReportsCommand(ActorUserId, "full", "بحث", null), CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Any<ReportsQaQuery>());
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LimitProvided_CapsMatchedReportCount()
    {
        FinalMediaReport[] reports = Enumerable.Range(1, 10).Select(i => FinalMediaReportTestData.BuildEntity(i)).ToArray();
        foreach (FinalMediaReport report in reports)
        {
            report.Title = "تقرير مشترك";
        }

        _dbContext.FinalMediaReports.Returns(reports.AsQueryable());

        Result<SearchFinalMediaReportsResultDto> result = await _handler.Handle(
            new SearchFinalMediaReportsCommand(ActorUserId, "full", "مشترك", 2), CancellationToken.None);

        result.Value!.Reports.Should().HaveCount(2);
    }
}
