namespace Icbank.Platform.Application.Campaigns;

/// <summary>One channel a campaign publishes through, with the reach it produced.</summary>
/// <param name="Name">The channel's Arabic display name.</param>
/// <param name="PublishedItems">The number of items published on this channel.</param>
/// <param name="ReachCount">How many people this channel reached.</param>
/// <param name="EngagementCount">How many interactions this channel drew.</param>
/// <param name="SharePercent">This channel's share of the campaign's total reach, 0-100.</param>
public sealed record CampaignChannelDto(
    string Name,
    int PublishedItems,
    int ReachCount,
    int EngagementCount,
    int SharePercent);
