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
/// Verifies <see cref="CreateFinalMediaReportCommandHandler"/> computes the report number and
/// content hash, persists a permanently-locked row, and writes an audit entry.
/// </summary>
public sealed class CreateFinalMediaReportCommandHandlerTests
{
    private const int ActorUserId = 9;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly CreateFinalMediaReportCommandHandler _handler;

    public CreateFinalMediaReportCommandHandlerTests()
    {
        _dateTimeProvider.RiyadhNow.Returns(new DateTimeOffset(2026, 7, 10, 12, 0, 0, TimeSpan.FromHours(3)));
        _dbContext.FinalMediaReports.Returns(Array.Empty<FinalMediaReport>().AsQueryable());
        _handler = new CreateFinalMediaReportCommandHandler(_dbContext, _queryExecutor, _auditLogService, _dateTimeProvider);
    }

    [Fact]
    public async Task Handle_NoExistingReportsThisYear_AssignsSequenceNumberOne()
    {
        var command = new CreateFinalMediaReportCommand(
            ActorUserId, "تقرير تجريبي", "Weekly", "الأسبوع الأول", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, FinalMediaReportTestData.BuildDraftDto());

        Result<FinalMediaReportDto> result = await _handler.Handle(command, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.ReportNumber.Should().Be("GAC-MEDIA-1/2026");
        result.Value.Status.Should().Be(nameof(FinalMediaReportStatus.Final));
    }

    [Fact]
    public async Task Handle_ExistingReportsThisYear_IncrementsSequenceNumber()
    {
        _dbContext.FinalMediaReports.Returns(new[] { FinalMediaReportTestData.BuildEntity(1, "GAC-MEDIA-4/2026") }.AsQueryable());
        var command = new CreateFinalMediaReportCommand(
            ActorUserId, "تقرير تجريبي", "Weekly", "الأسبوع الأول", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, FinalMediaReportTestData.BuildDraftDto());

        Result<FinalMediaReportDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.ReportNumber.Should().Be("GAC-MEDIA-5/2026");
    }

    [Fact]
    public async Task Handle_Always_PersistsRowAndWritesAuditEntry()
    {
        var command = new CreateFinalMediaReportCommand(
            ActorUserId, "تقرير تجريبي", "Weekly", "الأسبوع الأول", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, FinalMediaReportTestData.BuildDraftDto());

        await _handler.Handle(command, CancellationToken.None);

        _dbContext.Received(1).Add(Arg.Any<FinalMediaReport>());
        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "final_media_report.create", "FinalMediaReport", Arg.Any<string>(), Arg.Any<object?>(), Arg.Any<object?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_Always_ComputesNonEmptyContentHash()
    {
        var command = new CreateFinalMediaReportCommand(
            ActorUserId, "تقرير تجريبي", "Weekly", "الأسبوع الأول", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, FinalMediaReportTestData.BuildDraftDto());
        FinalMediaReport? persisted = null;
        _dbContext.When(context => context.Add(Arg.Any<FinalMediaReport>())).Do(callInfo => persisted = callInfo.Arg<FinalMediaReport>());

        await _handler.Handle(command, CancellationToken.None);

        persisted!.ContentSha256.Should().NotBeNullOrEmpty();
        persisted.ContentSha256.Should().HaveLength(64);
    }

    [Fact]
    public async Task Handle_UnrecognisedReportType_FallsBackToWeekly()
    {
        var command = new CreateFinalMediaReportCommand(
            ActorUserId, "تقرير تجريبي", "not-a-type", "الأسبوع الأول", DateTimeOffset.UtcNow.AddDays(-7), DateTimeOffset.UtcNow, FinalMediaReportTestData.BuildDraftDto());

        Result<FinalMediaReportDto> result = await _handler.Handle(command, CancellationToken.None);

        result.Value!.ReportType.Should().Be(nameof(MediaReportType.Weekly));
    }
}
