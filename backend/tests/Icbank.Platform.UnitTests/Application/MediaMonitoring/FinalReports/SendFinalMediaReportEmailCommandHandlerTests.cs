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

/// <summary>Verifies <see cref="SendFinalMediaReportEmailCommandHandler"/> preserves the honest no-op contract and always writes an audit entry, sent or not.</summary>
public sealed class SendFinalMediaReportEmailCommandHandlerTests
{
    private const int ActorUserId = 7;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IReportEmailSender _emailSender = Substitute.For<IReportEmailSender>();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly SendFinalMediaReportEmailCommandHandler _handler;

    public SendFinalMediaReportEmailCommandHandlerTests()
    {
        _handler = new SendFinalMediaReportEmailCommandHandler(_dbContext, _queryExecutor, _emailSender, _auditLogService);
    }

    [Fact]
    public async Task Handle_ProviderNotConfigured_ReturnsHonestNoOpNotFabricatedSuccess()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());
        _emailSender.SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportEmailResult(false, "لم يتم الإرسال"));

        Result<SendFinalMediaReportEmailResultDto> result = await _handler.Handle(
            new SendFinalMediaReportEmailCommand(ActorUserId, 1, new List<string> { "a@b.com" }, null), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Sent.Should().BeFalse();
        result.Value.ProviderMessage.Should().Be("لم يتم الإرسال");
    }

    [Fact]
    public async Task Handle_MissingReport_ReturnsFailureAndNeverCallsSender()
    {
        _dbContext.FinalMediaReports.Returns(Array.Empty<FinalMediaReport>().AsQueryable());

        Result<SendFinalMediaReportEmailResultDto> result = await _handler.Handle(
            new SendFinalMediaReportEmailCommand(ActorUserId, 404, new List<string> { "a@b.com" }, null), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        await _emailSender.DidNotReceive().SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Always_WritesAuditEntryRegardlessOfSendOutcome()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());
        _emailSender.SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportEmailResult(false, "لم يتم الإرسال"));

        await _handler.Handle(new SendFinalMediaReportEmailCommand(ActorUserId, 1, new List<string> { "a@b.com" }, null), CancellationToken.None);

        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "final_media_report.send_email", "FinalMediaReport", "1", Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SubjectOmitted_FallsBackToReportTitle()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());
        _emailSender.SendAsync(Arg.Any<IReadOnlyList<string>>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ReportEmailResult(true, "sent"));

        await _handler.Handle(new SendFinalMediaReportEmailCommand(ActorUserId, 1, new List<string> { "a@b.com" }, null), CancellationToken.None);

        await _emailSender.Received(1).SendAsync(Arg.Any<IReadOnlyList<string>>(), report.Title, Arg.Any<string>(), Arg.Any<CancellationToken>());
    }
}
