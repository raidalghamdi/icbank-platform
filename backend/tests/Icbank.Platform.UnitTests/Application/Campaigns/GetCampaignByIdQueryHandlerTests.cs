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
/// Verifies <see cref="GetCampaignByIdQueryHandler"/>: the detail page reads its own campaign so a
/// direct link from the dashboard works without the board payload, an untracked campaign reads as
/// missing, and only the requested campaign's own outputs and channels come back.
/// </summary>
public sealed class GetCampaignByIdQueryHandlerTests
{
    private static readonly DateTime Now = new(2026, 3, 1, 9, 0, 0, DateTimeKind.Utc);

    private readonly IApplicationDbContext _dbContext = Substitute.For<IApplicationDbContext>();
    private readonly TestAsyncQueryExecutor _queryExecutor = new();
    private readonly IDateTimeProvider _clock = Substitute.For<IDateTimeProvider>();
    private readonly GetCampaignByIdQueryHandler _handler;

    /// <summary>Initializes a new instance of the <see cref="GetCampaignByIdQueryHandlerTests"/> class.</summary>
    public GetCampaignByIdQueryHandlerTests()
    {
        _clock.UtcNow.Returns(new DateTimeOffset(Now));
        _handler = new GetCampaignByIdQueryHandler(_dbContext, _queryExecutor, _clock);
    }

    [Fact]
    public async Task Handle_UnknownId_FailsWithTheNotFoundError()
    {
        Arrange(Array.Empty<Campaign>());

        Result<CampaignDto> result = await _handler.Handle(new GetCampaignByIdQuery(99), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Be(GetCampaignByIdQuery.CampaignNotFoundError);
    }

    [Fact]
    public async Task Handle_UntrackedCampaign_FailsWithTheNotFoundError()
    {
        Campaign retired = GetCampaignBoardQueryHandlerTests.MakeCampaign(1, "INT-01");
        retired.IsActive = false;
        Arrange(new[] { retired });

        Result<CampaignDto> result = await _handler.Handle(new GetCampaignByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_TrackedCampaign_ReturnsOnlyItsOwnOutputsAndChannels()
    {
        Arrange(
            new[]
            {
                GetCampaignBoardQueryHandlerTests.MakeCampaign(1, "INT-01"),
                GetCampaignBoardQueryHandlerTests.MakeCampaign(2, "INT-02"),
            },
            new[]
            {
                GetCampaignBoardQueryHandlerTests.MakeDeliverable(20, 1, "مخرج الحملة", sortOrder: 1, isCompleted: true),
                GetCampaignBoardQueryHandlerTests.MakeDeliverable(21, 2, "مخرج حملة أخرى", sortOrder: 1, isCompleted: true),
            },
            new[]
            {
                GetCampaignBoardQueryHandlerTests.MakeChannel(30, 1, "البريد الداخلي", reach: 400),
                GetCampaignBoardQueryHandlerTests.MakeChannel(31, 2, "قناة حملة أخرى", reach: 900),
            });

        Result<CampaignDto> result = await _handler.Handle(new GetCampaignByIdQuery(1), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Code.Should().Be("INT-01");
        result.Value.Deliverables.Should().ContainSingle(d => d.Title == "مخرج الحملة");
        result.Value.Channels.Should().ContainSingle(c => c.Name == "البريد الداخلي");
    }

    [Fact]
    public async Task Handle_TrackedCampaign_CarriesTheAnalyticsAndTheScheduleForTheDetailPage()
    {
        Campaign campaign = GetCampaignBoardQueryHandlerTests.MakeCampaign(1, "EXT-01", CampaignAudience.External);
        campaign.StartDate = Now.AddDays(-10);
        campaign.EndDate = Now.AddDays(4);
        campaign.ImpressionsCount = 20_000;
        campaign.EngagementCount = 500;
        Arrange(new[] { campaign });

        Result<CampaignDto> result = await _handler.Handle(new GetCampaignByIdQuery(1), CancellationToken.None);

        result.Value!.DurationDays.Should().Be(15);
        result.Value.DaysRemaining.Should().Be(4);
        result.Value.Analytics.EngagementRatePerMille.Should().Be(25);
    }

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
