using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.MediaMonitoring;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>
/// Verifies the weekly report review workflow: a generated report is a draft that can be edited,
/// approved exactly once into the locked archive, and discarded only while it is still a draft;
/// exporting waits for approval.
/// </summary>
public sealed class FinalReportReviewWorkflowTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 26, 12, 0, 0, TimeSpan.FromHours(3));

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IAuditLogService _audit = Substitute.For<IAuditLogService>();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();

    /// <summary>Initializes a new instance of the <see cref="FinalReportReviewWorkflowTests"/> class.</summary>
    public FinalReportReviewWorkflowTests()
    {
        _clock.RiyadhNow.Returns(Now);
        _clock.UtcNow.Returns(Now.ToUniversalTime());
    }

    [Fact]
    public async Task Update_Draft_SavesTitleContentAndLayout()
    {
        FinalMediaReport report = Draft();
        var layout = "{\"sections\":[{\"key\":\"news\",\"visible\":true},{\"key\":\"contact_center\",\"visible\":true}],\"contactCenter\":{\"calls\":12}}";

        Result<FinalMediaReportDetailDto> result = await new UpdateFinalMediaReportDraftCommandHandler(_dbContext, _queryExecutor)
            .Handle(new UpdateFinalMediaReportDraftCommand(1, 1, "عنوان معدل", FinalMediaReportTestData.BuildDraftDto(), layout), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        report.Title.Should().Be("عنوان معدل");
        FinalReportLayout.Parse(report.LayoutJson).ContactCenter!.Calls.Should().Be(12);
        await _dbContext.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Update_ApprovedReport_IsRejectedAsLocked()
    {
        FinalMediaReport report = Draft();
        report.Status = FinalMediaReportStatus.Final;

        Result<FinalMediaReportDetailDto> result = await new UpdateFinalMediaReportDraftCommandHandler(_dbContext, _queryExecutor)
            .Handle(new UpdateFinalMediaReportDraftCommand(1, 1, "x", FinalMediaReportTestData.BuildDraftDto(), null), CancellationToken.None);

        result.Error.Should().Be(UpdateFinalMediaReportDraftCommand.LockedError);
    }

    [Fact]
    public async Task Approve_Draft_LocksItAsFinalWithTheApprover()
    {
        FinalMediaReport report = Draft();

        Result<FinalMediaReportDto> result = await new ApproveFinalMediaReportCommandHandler(_dbContext, _queryExecutor, _audit, _clock)
            .Handle(new ApproveFinalMediaReportCommand(1, 1, "مراجع"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        report.Status.Should().Be(FinalMediaReportStatus.Final);
        report.ApprovedByName.Should().Be("مراجع");
        report.ApprovedAt.Should().Be(Now);
    }

    [Fact]
    public async Task Approve_AlreadyApproved_Fails()
    {
        FinalMediaReport report = Draft();
        report.Status = FinalMediaReportStatus.Final;

        Result<FinalMediaReportDto> result = await new ApproveFinalMediaReportCommandHandler(_dbContext, _queryExecutor, _audit, _clock)
            .Handle(new ApproveFinalMediaReportCommand(1, 1, "مراجع"), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Discard_Draft_RemovesItAndReleasesTheNumber()
    {
        FinalMediaReport report = Draft();

        Result<bool> result = await new DiscardFinalMediaReportDraftCommandHandler(_dbContext, _queryExecutor)
            .Handle(new DiscardFinalMediaReportDraftCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        report.ReportNumber.Should().EndWith("-X1");
        _dbContext.Received(1).Remove(report);
    }

    [Fact]
    public async Task Discard_ApprovedReport_IsRefused()
    {
        FinalMediaReport report = Draft();
        report.Status = FinalMediaReportStatus.Final;

        Result<bool> result = await new DiscardFinalMediaReportDraftCommandHandler(_dbContext, _queryExecutor)
            .Handle(new DiscardFinalMediaReportDraftCommand(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        _dbContext.DidNotReceive().Remove(Arg.Any<FinalMediaReport>());
    }

    [Fact]
    public void Layout_HiddenAndReorderedSections_RenderInLayoutOrderOnly()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);
        var layout = FinalReportLayout.Parse(
            "{\"sections\":[{\"key\":\"recommendations\",\"title\":\"توصيات الأسبوع\",\"visible\":true},{\"key\":\"summary\",\"visible\":false},"
            + "{\"key\":\"contact_center\",\"visible\":true},{\"key\":\"custom-1\",\"title\":\"قسم مضاف\",\"visible\":true,\"body\":\"نص القسم\"}],"
            + "\"contactCenter\":{\"calls\":40,\"emails\":7,\"inquiries\":3,\"notes\":\"استفسارات الاندماج\"}}");

        var html = FinalReportHtmlBuilder.Build(detail, layout);

        html.Should().Contain("توصيات الأسبوع").And.Contain("مركز الاتصال").And.Contain("قسم مضاف").And.Contain("نص القسم").And.Contain("استفسارات الاندماج");
        html.Should().NotContain("الملخص التنفيذي");
        html.IndexOf("توصيات الأسبوع", StringComparison.Ordinal).Should().BeLessThan(html.IndexOf("قسم مضاف", StringComparison.Ordinal));
    }

    [Fact]
    public void Layout_InvalidJson_IsReported()
    {
        FinalReportLayout.Validate("{not json").Should().NotBeNull();
        FinalReportLayout.Validate(null).Should().BeNull();
    }

    private FinalMediaReport Draft()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        report.Status = FinalMediaReportStatus.Draft;
        _dbContext.FinalMediaReports.Returns(new[] { report }.AsQueryable());
        return report;
    }
}
