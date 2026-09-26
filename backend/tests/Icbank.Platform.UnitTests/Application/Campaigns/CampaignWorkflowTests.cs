using FluentAssertions;
using Icbank.Platform.Application.Campaigns;
using Icbank.Platform.Application.Campaigns.Commands;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Domain.Campaigns;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Campaigns;

/// <summary>
/// Verifies the campaign lifecycle: progress follows completed tasks and deliverables, the board
/// status follows the stage, a campaign cannot close before measurement, and an approved request
/// becomes a campaign with the same details.
/// </summary>
public sealed class CampaignWorkflowTests
{
    private static readonly string[] UnknownSupport = { "unknown" };

    private static readonly DateTime Start = new(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();

    [Fact]
    public void Progress_CountsCompletedTasksAndDeliverables()
    {
        System.Text.Json.JsonElement plan = CampaignPlan.ToElement("{\"tasks\":[{\"done\":true},{\"done\":false}],\"deliverables\":[{\"done\":true},{\"done\":true}]}")!.Value;

        CampaignPlan.Progress(plan).Should().Be(75);
        CampaignPlan.Progress(CampaignPlan.ToElement("{}")!.Value).Should().BeNull();
    }

    [Fact]
    public async Task Save_NewCampaign_DerivesStatusAndProgressFromThePlan()
    {
        _dbContext.Campaigns.Returns(Array.Empty<Campaign>().AsQueryable());
        Campaign? added = null;
        _dbContext.When(d => d.Add(Arg.Any<Campaign>())).Do(call => added = call.Arg<Campaign>());

        Result<int> result = await Handler().Handle(
            new SaveCampaignCommand(null, "external", Fields("execution", "{\"tasks\":[{\"done\":true},{\"done\":false}],\"results\":{\"reach\":900}}")),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        added!.Audience.Should().Be(CampaignAudience.External);
        added.Status.Should().Be(CampaignStatus.Running);
        added.ProgressPercent.Should().Be(50);
        added.ReachCount.Should().Be(900);
        added.IsUserManaged.Should().BeTrue();
    }

    [Fact]
    public async Task Save_ClosingBeforeMeasurement_IsRefused()
    {
        Campaign campaign = GetCampaignBoardQueryHandlerTests.MakeCampaign(1, "INT-01");
        campaign.Stage = CampaignStage.Execution;
        _dbContext.Campaigns.Returns(new[] { campaign }.AsQueryable());

        Result<int> result = await Handler().Handle(new SaveCampaignCommand(1, "internal", Fields("closed", null)), CancellationToken.None);

        result.Error.Should().Be(SaveCampaignCommand.ClosureError);
    }

    [Fact]
    public async Task Save_MeasurementStage_IsListedUnderReviewNotCompleted()
    {
        Campaign campaign = GetCampaignBoardQueryHandlerTests.MakeCampaign(1, "INT-01");
        campaign.Stage = CampaignStage.Execution;
        _dbContext.Campaigns.Returns(new[] { campaign }.AsQueryable());

        await Handler().Handle(new SaveCampaignCommand(1, "internal", Fields("measurement", null)), CancellationToken.None);

        campaign.Status.Should().Be(CampaignStatus.UnderReview);
    }

    [Fact]
    public async Task Save_InvertedPeriodOrBadPlan_IsRefused()
    {
        _dbContext.Campaigns.Returns(Array.Empty<Campaign>().AsQueryable());
        SaveCampaignFields inverted = Fields("planning", null) with { EndDate = Start.AddDays(-1) };

        (await Handler().Handle(new SaveCampaignCommand(null, "internal", inverted), CancellationToken.None)).Error.Should().Be(SaveCampaignCommand.PeriodError);
        (await Handler().Handle(new SaveCampaignCommand(null, "internal", Fields("planning", "[1]")), CancellationToken.None)).Error.Should().Be(SaveCampaignCommand.PlanError);
    }

    [Fact]
    public async Task Review_Approve_ConvertsTheRequestIntoAPlanningCampaign()
    {
        var request = new CampaignRequest
        {
            Id = 7,
            RequestingDepartment = "إدارة الموارد البشرية",
            Name = "حملة السلامة",
            Objective = "رفع الوعي",
            TargetAudience = "جميع الموظفين",
            ProposedStart = Start,
            ProposedEnd = Start.AddDays(20),
            KeyMessages = "السلامة أولًا",
            SupportTypes = "content,design",
        };
        _dbContext.CampaignRequests.Returns(new[] { request }.AsQueryable());
        Campaign? added = null;
        _dbContext.When(d => d.Add(Arg.Any<Campaign>())).Do(call => added = call.Arg<Campaign>());
        IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();

        Result<CampaignRequestDto> result = await new ReviewCampaignRequestCommandHandler(_dbContext, _queryExecutor, clock)
            .Handle(new ReviewCampaignRequestCommand(7, true, null, "الاتصال المؤسسي"), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        request.Status.Should().Be(CampaignRequestStatus.Approved);
        added!.Name.Should().Be("حملة السلامة");
        added.TargetAudience.Should().Be("جميع الموظفين");
        added.Stage.Should().Be(CampaignStage.Planning);
        added.SourceRequestId.Should().Be(7);
        CampaignPlan.Deliverables(CampaignPlan.ToElement(added.PlanJson))!.Select(d => d.Title).Should().Equal("المحتوى", "التصميم");
    }

    [Fact]
    public async Task Review_ReturnWithoutNote_IsRefused()
    {
        _dbContext.CampaignRequests.Returns(new[] { new CampaignRequest { Id = 1 } }.AsQueryable());

        Result<CampaignRequestDto> result = await new ReviewCampaignRequestCommandHandler(_dbContext, _queryExecutor, Substitute.For<IDateTimeProvider>())
            .Handle(new ReviewCampaignRequestCommand(1, false, " ", "مراجع"), CancellationToken.None);

        result.Error.Should().Be(ReviewCampaignRequestCommand.NoteRequiredError);
    }

    [Fact]
    public async Task Submit_WithoutSupportType_IsRefused()
    {
        Result<CampaignRequestDto> result = await new SubmitCampaignRequestCommandHandler(_dbContext).Handle(
            new SubmitCampaignRequestCommand(1, "موظف", "إدارة", "اسم", "هدف", "جمهور", Start, Start, string.Empty, UnknownSupport),
            CancellationToken.None);

        result.Error.Should().Be(SubmitCampaignRequestCommand.SupportError);
    }

    [Fact]
    public async Task Upload_UnsupportedType_IsRefusedAndNothingIsStored()
    {
        IObjectStorageWriter storage = Substitute.For<IObjectStorageWriter>();

        Result<CampaignFileDto> result = await new UploadCampaignFileCommandHandler(storage).Handle(
            new UploadCampaignFileCommand(1, "a.exe", "application/x-msdownload", "AAAA"), CancellationToken.None);

        result.Error.Should().Be(UploadCampaignFileCommand.TypeError);
        await storage.DidNotReceiveWithAnyArgs().SaveAsync(default!, default!, default!, default);
    }

    private static SaveCampaignFields Fields(string stage, string? planJson) => new(
        "حملة", "هدف", "جمهور", Start, Start.AddDays(10), "رسالة", "مالك", "عضو", "X", stage, planJson, null);

    private SaveCampaignCommandHandler Handler() => new(_dbContext, _queryExecutor);
}
