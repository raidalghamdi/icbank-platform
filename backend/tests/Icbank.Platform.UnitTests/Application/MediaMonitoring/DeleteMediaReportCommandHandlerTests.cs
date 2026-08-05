using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="DeleteMediaReportCommandHandler"/> hard-deletes and writes an audit entry on success, and fails cleanly on a missing id (SEC-16 existence check).</summary>
public sealed class DeleteMediaReportCommandHandlerTests
{
    private const int ActorUserId = 3;

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _auditLogService = Substitute.For<IAuditLogService>();
    private readonly DeleteMediaReportCommandHandler _handler;

    public DeleteMediaReportCommandHandlerTests()
    {
        _handler = new DeleteMediaReportCommandHandler(_dbContext, _queryExecutor, _auditLogService);
    }

    [Fact]
    public async Task Handle_ExistingReport_RemovesAndWritesAuditEntry()
    {
        var report = new MediaReport { Id = 7, Title = "To Delete", ContentMd = "c" };
        _dbContext.MediaReports.Returns(new[] { report }.AsQueryable());

        Result<bool> result = await _handler.Handle(new DeleteMediaReportCommand(ActorUserId, 7), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _dbContext.Received(1).Remove(report);
        await _auditLogService.Received(1).RecordAsync(
            ActorUserId, "media_report.delete", "MediaReport", "7", Arg.Any<object>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MissingReport_ReturnsFailureAndDoesNotRemoveOrAudit()
    {
        _dbContext.MediaReports.Returns(Array.Empty<MediaReport>().AsQueryable());

        Result<bool> result = await _handler.Handle(new DeleteMediaReportCommand(ActorUserId, 404), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _dbContext.DidNotReceive().Remove(Arg.Any<MediaReport>());
        await _auditLogService.DidNotReceive().RecordAsync(
            Arg.Any<int>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<object>(), Arg.Any<CancellationToken>());
    }
}
