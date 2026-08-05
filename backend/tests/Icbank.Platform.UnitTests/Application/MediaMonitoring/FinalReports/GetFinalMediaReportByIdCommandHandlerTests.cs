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

/// <summary>
/// Verifies <see cref="GetFinalMediaReportByIdCommandHandler"/> awaits the view-count increment
/// inline (closing the Node source's fire-and-forget race) and returns 404-style failure on a
/// missing id.
/// </summary>
public sealed class GetFinalMediaReportByIdCommandHandlerTests
{
    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly GetFinalMediaReportByIdCommandHandler _handler;

    public GetFinalMediaReportByIdCommandHandlerTests()
    {
        _handler = new GetFinalMediaReportByIdCommandHandler(_dbContext, _queryExecutor);
    }

    [Fact]
    public async Task Handle_ExistingReport_IncrementsViewCountAndReturnsDetail()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        report.ViewCount = 4;
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());

        Result<FinalMediaReportDetailDto> result = await _handler.Handle(new GetFinalMediaReportByIdCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Summary.ViewCount.Should().Be(5);
        report.ViewCount.Should().Be(5);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingReport_ReturnsFailureWithArabicMessage()
    {
        _dbContext.FinalMediaReports.Returns(Array.Empty<FinalMediaReport>().AsQueryable());

        Result<FinalMediaReportDetailDto> result = await _handler.Handle(new GetFinalMediaReportByIdCommand(404), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be("التقرير غير موجود");
    }

    [Fact]
    public async Task Handle_ExistingReport_MapsAllEightSections()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(2);
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());

        Result<FinalMediaReportDetailDto> result = await _handler.Handle(new GetFinalMediaReportByIdCommand(2), CancellationToken.None);

        result.Value!.TopNews.Should().NotBeEmpty();
        result.Value.Timeline.Should().NotBeEmpty();
        result.Value.DigitalPresence.Platforms.Should().NotBeEmpty();
        result.Value.EditorialTone.Distribution.Should().NotBeEmpty();
        result.Value.DeepAnalysis.Keywords.Should().NotBeEmpty();
        result.Value.RegionalComparison.Should().NotBeEmpty();
        result.Value.Recommendations.Should().NotBeEmpty();
        result.Value.Alerts.Should().NotBeEmpty();
        result.Value.QuotesAppendix.Should().NotBeEmpty();
        result.Value.Sources.Should().NotBeEmpty();
    }
}
