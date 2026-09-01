using Icbank.Platform.Domain.Campaigns;

namespace Icbank.Platform.Application.Campaigns;

/// <summary>
/// Shared entity-to-DTO projection for a campaign. The board query and the detail query hand the
/// browser the same shape, so the schedule, output-tally and analytics logic lives here once
/// instead of drifting between the two call sites.
/// </summary>
public static class CampaignMapper
{
    private const int PerMille = 1000;

    /// <summary>Projects a campaign, its outputs and its channels onto the DTO both pages render.</summary>
    /// <param name="campaign">The tracked campaign.</param>
    /// <param name="deliverables">The campaign's headline outputs, in display order.</param>
    /// <param name="channels">The campaign's channels, in any order.</param>
    /// <param name="now">The current UTC instant, used to place the campaign against its schedule.</param>
    /// <returns>The campaign as the pages render it.</returns>
    public static CampaignDto ToDto(
        Campaign campaign,
        IReadOnlyCollection<CampaignDeliverable> deliverables,
        IReadOnlyCollection<CampaignChannel> channels,
        DateTime now)
    {
        ArgumentNullException.ThrowIfNull(campaign);
        ArgumentNullException.ThrowIfNull(deliverables);
        ArgumentNullException.ThrowIfNull(channels);

        return new CampaignDto(
            campaign.Id,
            campaign.Code,
            campaign.Name,
            campaign.Description,
            campaign.Objective,
            CampaignLabels.AudienceKey(campaign.Audience),
            CampaignLabels.AudienceLabel(campaign.Audience),
            CampaignLabels.StatusKey(campaign.Status),
            CampaignLabels.StatusLabel(campaign.Status),
            campaign.Owner,
            campaign.Department,
            campaign.ProgressPercent,
            campaign.StartDate,
            campaign.EndDate,
            DurationDays(campaign.StartDate, campaign.EndDate),
            DaysRemaining(campaign.EndDate, now),
            campaign.LatestUpdate,
            deliverables.Count(d => d.IsCompleted),
            deliverables.Count,
            deliverables.Select(ToDto).ToList(),
            MapChannels(channels),
            ToAnalytics(campaign));
    }

    /// <summary>Computes the campaign's length in days, counting both the first and the last day.</summary>
    /// <param name="startDate">The UTC start date.</param>
    /// <param name="endDate">The UTC end date.</param>
    /// <returns>The number of days, never below one.</returns>
    public static int DurationDays(DateTime startDate, DateTime endDate)
    {
        var span = (int)Math.Round((endDate.Date - startDate.Date).TotalDays, MidpointRounding.AwayFromZero);
        return Math.Max(1, span + 1);
    }

    /// <summary>Computes how many days are left before the campaign closes.</summary>
    /// <param name="endDate">The UTC end date.</param>
    /// <param name="now">The current UTC instant.</param>
    /// <returns>Days remaining; negative once the end date has passed.</returns>
    public static int DaysRemaining(DateTime endDate, DateTime now)
        => (int)Math.Round((endDate.Date - now.Date).TotalDays, MidpointRounding.AwayFromZero);

    private static CampaignDeliverableDto ToDto(CampaignDeliverable deliverable)
        => new(deliverable.Title, deliverable.DueDate, deliverable.IsCompleted);

    // Why: the share is computed against the summed channel reach rather than the campaign's own
    // reach figure. The campaign total is a de-duplicated audience estimate and is deliberately
    // smaller than the sum of its channels, so dividing by it would push the bars past 100%.
    private static List<CampaignChannelDto> MapChannels(IReadOnlyCollection<CampaignChannel> channels)
    {
        var totalReach = channels.Sum(c => c.ReachCount);
        return channels
            .OrderByDescending(c => c.ReachCount)
            .ThenBy(c => c.SortOrder)
            .Select(c => new CampaignChannelDto(
                c.Name,
                c.PublishedItems,
                c.ReachCount,
                c.EngagementCount,
                Percent(c.ReachCount, totalReach)))
            .ToList();
    }

    private static CampaignAnalyticsDto ToAnalytics(Campaign campaign)
        => new(
            campaign.ReachCount,
            campaign.ImpressionsCount,
            campaign.EngagementCount,
            campaign.PublishedItems,
            PerMilleRate(campaign.EngagementCount, campaign.ImpressionsCount));

    private static int Percent(int part, int whole)
        => whole <= 0 ? 0 : (int)Math.Round(part * 100d / whole, MidpointRounding.AwayFromZero);

    private static int PerMilleRate(int part, int whole)
        => whole <= 0 ? 0 : (int)Math.Round(part * (double)PerMille / whole, MidpointRounding.AwayFromZero);
}
