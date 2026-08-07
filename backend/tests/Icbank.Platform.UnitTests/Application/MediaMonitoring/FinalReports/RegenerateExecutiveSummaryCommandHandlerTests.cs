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

/// <summary>Verifies <see cref="RegenerateExecutiveSummaryCommandHandler"/> never persists the regenerated text (matches the Node source exactly) and writes an audit entry.</summary>
public sealed class RegenerateExecutiveSummaryCommandHandlerTests
{
    private const int ActorUserId = 6;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IExecutiveSummaryRegenerator _regenerator = Substitute.For<IExecutiveSummaryRegenerator>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly RegenerateExecutiveSummaryCommandHandler _handler;

    public RegenerateExecutiveSummaryCommandHandlerTests()
    {
        _handler = new RegenerateExecutiveSummaryCommandHandler(_dbContext, _queryExecutor, _regenerator, _auditLogService);
    }

    [Fact]
    public async Task Handle_ExistingReport_ReturnsRegeneratedSummaryAndReportNumberWithoutPersisting()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(3, "GAC-MEDIA-9/2026");
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());
        _regenerator.RegenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("ملخص جديد");

        Result<RegenerateExecutiveSummaryResultDto> result = await _handler.Handle(new RegenerateExecutiveSummaryCommand(ActorUserId, 3), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Summary.Should().Be("ملخص جديد");
        result.Value.ReportNumber.Should().Be("GAC-MEDIA-9/2026");
        await _dbContext.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingReport_ReturnsFailureAndNeverCallsRegenerator()
    {
        _dbContext.FinalMediaReports.Returns(Array.Empty<FinalMediaReport>().AsQueryable());

        Result<RegenerateExecutiveSummaryResultDto> result = await _handler.Handle(new RegenerateExecutiveSummaryCommand(ActorUserId, 404), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _regenerator.DidNotReceive().RegenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExistingReport_WritesAuditEntry()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(3);
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());
        _regenerator.RegenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("ملخص");

        await _handler.Handle(new RegenerateExecutiveSummaryCommand(ActorUserId, 3), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "final_media_report.exec_summary_regenerate", "FinalMediaReport", "3", null, null, Arg.Any<CancellationToken>());
    }
}
