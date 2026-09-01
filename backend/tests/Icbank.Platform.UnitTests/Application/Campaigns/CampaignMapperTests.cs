using FluentAssertions;
using Icbank.Platform.Application.Campaigns;
using Icbank.Platform.Domain.Campaigns;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Campaigns;

/// <summary>
/// Verifies <see cref="CampaignMapper"/>: the figures the campaign cards and the detail page print
/// are resolved once here, so both surfaces quote the same duration, the same channel shares and
/// the same engagement rate.
/// </summary>
public sealed class CampaignMapperTests
{
    private static readonly DateTime Now = new(2026, 3, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void DurationDays_SingleDayCampaign_CountsAsOneDayNotZero()
    {
        DateTime day = new(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);

        CampaignMapper.DurationDays(day, day).Should().Be(1);
    }

    [Fact]
    public void DurationDays_Always_CountsBothTheFirstAndTheLastDay()
    {
        CampaignMapper.DurationDays(Now, Now.AddDays(29)).Should().Be(30);
    }

    [Fact]
    public void DurationDays_EndBeforeStart_NeverReturnsBelowOne()
    {
        CampaignMapper.DurationDays(Now, Now.AddDays(-10)).Should().Be(1);
    }

    [Fact]
    public void DaysRemaining_PastEndDate_GoesNegativeSoTheCardCanSayOverdue()
    {
        CampaignMapper.DaysRemaining(Now.AddDays(-3), Now).Should().Be(-3);
    }

    [Fact]
    public void ToDto_ChannelsReachingMoreThanTheCampaignTotal_KeepsEveryShareWithin100Percent()
    {
        // The campaign figure is a de-duplicated audience estimate, so it is deliberately smaller
        // than the sum of its channels; dividing by it would push the bars past 100%.
        Campaign campaign = MakeCampaign(reach: 500);
        CampaignChannel[] channels =
        {
            MakeChannel("منصة إكس", reach: 600, sortOrder: 1),
            MakeChannel("لينكدإن", reach: 400, sortOrder: 2),
        };

        CampaignDto dto = CampaignMapper.ToDto(campaign, Array.Empty<CampaignDeliverable>(), channels, Now);

        dto.Channels.Select(c => c.SharePercent).Should().OnlyContain(share => share <= 100);
        dto.Channels.Sum(c => c.SharePercent).Should().Be(100);
    }

    [Fact]
    public void ToDto_Always_OrdersChannelsByReachSoTheWidestReadsFirst()
    {
        CampaignChannel[] channels =
        {
            MakeChannel("قناة صغيرة", reach: 100, sortOrder: 1),
            MakeChannel("قناة كبيرة", reach: 900, sortOrder: 2),
        };

        CampaignDto dto = CampaignMapper.ToDto(MakeCampaign(reach: 1000), Array.Empty<CampaignDeliverable>(), channels, Now);

        dto.Channels.Select(c => c.Name).Should().ContainInOrder("قناة كبيرة", "قناة صغيرة");
    }

    [Fact]
    public void ToDto_CampaignWithNoImpressionsYet_ReportsAZeroRateInsteadOfDividingByZero()
    {
        Campaign campaign = MakeCampaign(reach: 0);
        campaign.ImpressionsCount = 0;
        campaign.EngagementCount = 0;

        CampaignDto dto = CampaignMapper.ToDto(campaign, Array.Empty<CampaignDeliverable>(), Array.Empty<CampaignChannel>(), Now);

        dto.Analytics.EngagementRatePerMille.Should().Be(0);
        dto.Channels.Should().BeEmpty();
    }

    [Fact]
    public void ToDto_SubPercentEngagement_StillReadsAsAWholeNumberPerThousand()
    {
        // 0.4% would round to 0% and read as "no engagement at all"; per-mille keeps it visible.
        Campaign campaign = MakeCampaign(reach: 1000);
        campaign.ImpressionsCount = 250_000;
        campaign.EngagementCount = 1_000;

        CampaignDto dto = CampaignMapper.ToDto(campaign, Array.Empty<CampaignDeliverable>(), Array.Empty<CampaignChannel>(), Now);

        dto.Analytics.EngagementRatePerMille.Should().Be(4);
    }

    [Fact]
    public void ToDto_Always_TalliesTheDeliveredOutputsAgainstTheTotal()
    {
        CampaignDeliverable[] deliverables =
        {
            MakeDeliverable("مخرج مكتمل", isCompleted: true),
            MakeDeliverable("مخرج قائم", isCompleted: false),
            MakeDeliverable("مخرج آخر مكتمل", isCompleted: true),
        };

        CampaignDto dto = CampaignMapper.ToDto(MakeCampaign(reach: 10), deliverables, Array.Empty<CampaignChannel>(), Now);

        dto.DeliverablesTotal.Should().Be(3);
        dto.DeliverablesCompleted.Should().Be(2);
    }

    private static Campaign MakeCampaign(int reach)
        => new()
        {
            Id = 1,
            Code = "INT-01",
            Name = "حملة",
            Description = "وصف",
            Objective = "هدف",
            Audience = CampaignAudience.Internal,
            Status = CampaignStatus.Running,
            Owner = "مسؤول",
            Department = "إدارة",
            ProgressPercent = 50,
            StartDate = Now.AddDays(-10),
            EndDate = Now.AddDays(10),
            LatestUpdate = "تحديث",
            ReachCount = reach,
            ImpressionsCount = 10_000,
            EngagementCount = 200,
            PublishedItems = 5,
            SortOrder = 1,
            IsActive = true,
        };

    private static CampaignChannel MakeChannel(string name, int reach, int sortOrder)
        => new()
        {
            CampaignId = 1,
            Name = name,
            PublishedItems = 2,
            ReachCount = reach,
            EngagementCount = 10,
            SortOrder = sortOrder,
        };

    private static CampaignDeliverable MakeDeliverable(string title, bool isCompleted)
        => new()
        {
            CampaignId = 1,
            Title = title,
            DueDate = Now.AddDays(5),
            IsCompleted = isCompleted,
            SortOrder = 1,
        };
}
