using FluentAssertions;
using Icbank.Platform.Application.Campaigns;
using Icbank.Platform.Application.Campaigns.Queries;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Domain.Campaigns;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Campaigns;

/// <summary>
/// Verifies <see cref="GetCampaignBoardQueryHandler"/>: each campaigns page receives only its own
/// audience, the status filter narrows the list without moving the headline figures or the chip
/// counts, and the outputs and channels arrive batched onto their owning campaign rather than
/// fetched per card.
/// </summary>
public sealed class GetCampaignBoardQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly GetCampaignBoardQueryHandler _handler;

    /// <summary>Initializes a new instance of the <see cref="GetCampaignBoardQueryHandlerTests"/> class.</summary>
    public GetCampaignBoardQueryHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(Now));
        _handler = new GetCampaignBoardQueryHandler(_dbContext, _queryExecutor, _clock);
    }

    [Fact]
    public async Task Handle_NoCampaigns_ReturnsEmptyBoardWithZeroedKpis()
    {
        Arrange(Array.Empty<Campaign>());

        Result<CampaignBoardDto> result = await Handle("internal", null);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Campaigns.Should().BeEmpty();
        result.Value.Kpis.Total.Should().Be(0);
        result.Value.Kpis.AverageProgressPercent.Should().Be(0);
        result.Value.StatusCounts["all"].Should().Be(0);
    }

    [Fact]
    public async Task Handle_InternalAudience_ExcludesExternalCampaigns()
    {
        Arrange(new[]
        {
            MakeCampaign(1, "INT-01"),
            MakeCampaign(2, "EXT-01", CampaignAudience.External),
        });

        Result<CampaignBoardDto> result = await Handle("internal", null);

        result.Value!.Campaigns.Should().ContainSingle(c => c.Code == "INT-01");
        result.Value.Campaigns.Should().OnlyContain(c => c.Audience == "internal");
    }

    [Fact]
    public async Task Handle_UnknownAudience_ReadsBothBooksOfWork()
    {
        Arrange(new[]
        {
            MakeCampaign(1, "INT-01"),
            MakeCampaign(2, "EXT-01", CampaignAudience.External),
        });

        Result<CampaignBoardDto> result = await Handle(null, null);

        result.Value!.Campaigns.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_InactiveCampaign_IsExcluded()
    {
        Campaign hidden = MakeCampaign(1, "INT-01");
        hidden.IsActive = false;
        Arrange(new[] { hidden, MakeCampaign(2, "INT-02") });

        Result<CampaignBoardDto> result = await Handle("internal", null);

        result.Value!.Campaigns.Should().ContainSingle(c => c.Code == "INT-02");
    }

    [Fact]
    public async Task Handle_MixedStatuses_OrdersRunningFirstThenBySortOrder()
    {
        Arrange(new[]
        {
            MakeCampaign(1, "INT-03", status: CampaignStatus.Completed, sortOrder: 1),
            MakeCampaign(2, "INT-02", status: CampaignStatus.Running, sortOrder: 2),
            MakeCampaign(3, "INT-01", status: CampaignStatus.Running, sortOrder: 1),
        });

        Result<CampaignBoardDto> result = await Handle("internal", null);

        result.Value!.Campaigns.Select(c => c.Code).Should().ContainInOrder("INT-01", "INT-02", "INT-03");
    }

    [Fact]
    public async Task Handle_StatusFilter_NarrowsTheListButLeavesTheKpisOverTheWholeAudience()
    {
        Arrange(new[]
        {
            MakeCampaign(1, "INT-01", status: CampaignStatus.Running, progressPercent: 60),
            MakeCampaign(2, "INT-02", status: CampaignStatus.Upcoming, progressPercent: 20),
            MakeCampaign(3, "INT-03", status: CampaignStatus.Completed, progressPercent: 100),
        });

        Result<CampaignBoardDto> result = await Handle("internal", "running");

        result.Value!.Campaigns.Should().ContainSingle(c => c.Code == "INT-01");
        result.Value.Kpis.Total.Should().Be(3);
        result.Value.Kpis.Running.Should().Be(1);
        result.Value.Kpis.Upcoming.Should().Be(1);
        result.Value.Kpis.Completed.Should().Be(1);
        result.Value.Kpis.AverageProgressPercent.Should().Be(60);
    }

    [Fact]
    public async Task Handle_StatusFilter_LeavesEveryChipCountOverTheWholeAudience()
    {
        Arrange(new[]
        {
            MakeCampaign(1, "INT-01", status: CampaignStatus.Running),
            MakeCampaign(2, "INT-02", status: CampaignStatus.UnderReview),
            MakeCampaign(3, "INT-03", status: CampaignStatus.UnderReview),
        });

        Result<CampaignBoardDto> result = await Handle("internal", "under_review");

        result.Value!.Campaigns.Should().HaveCount(2);
        result.Value.StatusCounts["all"].Should().Be(3);
        result.Value.StatusCounts["running"].Should().Be(1);
        result.Value.StatusCounts["under_review"].Should().Be(2);
        result.Value.StatusCounts["upcoming"].Should().Be(0);
        result.Value.StatusCounts["completed"].Should().Be(0);
    }

    [Fact]
    public async Task Handle_AllStatusKeyword_AppliesNoFilter()
    {
        Arrange(new[]
        {
            MakeCampaign(1, "INT-01", status: CampaignStatus.Running),
            MakeCampaign(2, "INT-02", status: CampaignStatus.Completed),
        });

        Result<CampaignBoardDto> result = await Handle("internal", "all");

        result.Value!.Campaigns.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_Always_GroupsOutputsAndChannelsOntoTheirOwningCampaign()
    {
        Arrange(
            new[] { MakeCampaign(1, "INT-01"), MakeCampaign(2, "INT-02") },
            new[]
            {
                MakeDeliverable(20, 1, "ثانية", sortOrder: 2, isCompleted: false),
                MakeDeliverable(21, 1, "أولى", sortOrder: 1, isCompleted: true),
                MakeDeliverable(22, 2, "لحملة أخرى", sortOrder: 1, isCompleted: true),
            },
            new[]
            {
                MakeChannel(30, 1, "البريد الداخلي", reach: 200),
                MakeChannel(31, 1, "الشاشات الداخلية", reach: 600),
                MakeChannel(32, 2, "لحملة أخرى", reach: 50),
            });

        Result<CampaignBoardDto> result = await Handle("internal", null);

        CampaignDto first = result.Value!.Campaigns.Single(c => c.Code == "INT-01");
        first.Deliverables.Select(d => d.Title).Should().ContainInOrder("أولى", "ثانية");
        first.DeliverablesTotal.Should().Be(2);
        first.DeliverablesCompleted.Should().Be(1);
        first.Channels.Select(c => c.Name).Should().ContainInOrder("الشاشات الداخلية", "البريد الداخلي");
        result.Value.Campaigns.Single(c => c.Code == "INT-02").Channels.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_Always_ProjectsTheArabicLabelsAndMachineKeys()
    {
        Arrange(new[] { MakeCampaign(1, "EXT-01", CampaignAudience.External, CampaignStatus.UnderReview) });

        Result<CampaignBoardDto> result = await Handle("external", null);

        CampaignDto card = result.Value!.Campaigns.Single();
        card.Audience.Should().Be("external");
        card.AudienceLabel.Should().Be("خارجية");
        card.Status.Should().Be("under_review");
        card.StatusLabel.Should().Be("تحت المراجعة");
    }

    [Fact]
    public async Task Handle_Always_SumsTheReachAcrossTheAudienceIntoTheKpiRow()
    {
        Campaign first = MakeCampaign(1, "EXT-01", CampaignAudience.External);
        first.ReachCount = 120_000;
        Campaign second = MakeCampaign(2, "EXT-02", CampaignAudience.External);
        second.ReachCount = 80_000;
        Arrange(new[] { first, second });

        Result<CampaignBoardDto> result = await Handle("external", null);

        result.Value!.Kpis.TotalReach.Should().Be(200_000);
    }

    [Fact]
    public async Task Handle_Always_StampsTheResponseWithTheCurrentInstant()
    {
        Arrange(Array.Empty<Campaign>());

        Result<CampaignBoardDto> result = await Handle("internal", null);

        result.Value!.GeneratedAt.Should().Be(Now);
    }

    internal static Campaign MakeCampaign(
        int id,
        string code,
        CampaignAudience audience = CampaignAudience.Internal,
        CampaignStatus status = CampaignStatus.Running,
        int progressPercent = 50,
        int sortOrder = 1)
        => new()
        {
            Id = id,
            Code = code,
            Name = "حملة " + code,
            Description = "وصف",
            Objective = "هدف",
            Audience = audience,
            Status = status,
            Owner = "مسؤول",
            Department = "إدارة",
            ProgressPercent = progressPercent,
            StartDate = Now.AddDays(-30),
            EndDate = Now.AddDays(30),
            LatestUpdate = "تحديث",
            ReachCount = 1000,
            ImpressionsCount = 20_000,
            EngagementCount = 400,
            PublishedItems = 8,
            SortOrder = sortOrder,
            IsActive = true,
        };

    internal static CampaignDeliverable MakeDeliverable(int id, int campaignId, string title, int sortOrder, bool isCompleted)
        => new()
        {
            Id = id,
            CampaignId = campaignId,
            Title = title,
            DueDate = Now.AddDays(10),
            IsCompleted = isCompleted,
            SortOrder = sortOrder,
        };

    internal static CampaignChannel MakeChannel(int id, int campaignId, string name, int reach)
        => new()
        {
            Id = id,
            CampaignId = campaignId,
            Name = name,
            PublishedItems = 3,
            ReachCount = reach,
            EngagementCount = 40,
            SortOrder = id,
        };

    private Task<Result<CampaignBoardDto>> Handle(string? audience, string? status)
        => _handler.Handle(new GetCampaignBoardQuery(audience, status), CancellationToken.None);

    private void Arrange(
        IReadOnlyCollection<Campaign> campaigns,
        IReadOnlyCollection<CampaignDeliverable>? deliverables = null,
        IReadOnlyCollection<CampaignChannel>? channels = null)
    {
        _dbContext.Campaigns.Returns(campaigns.AsQueryable());
        _dbContext.CampaignDeliverables.Returns((deliverables ?? Array.Empty<CampaignDeliverable>()).AsQueryable());
        _dbContext.CampaignChannels.Returns((channels ?? Array.Empty<CampaignChannel>()).AsQueryable());
    }
}
