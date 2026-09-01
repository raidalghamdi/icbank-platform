namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>A publishing channel inside a <see cref="CampaignSeedRow"/>.</summary>
/// <param name="Name">The channel's Arabic display name.</param>
/// <param name="PublishedItems">The number of items published on this channel.</param>
/// <param name="ReachCount">How many people this channel reached.</param>
/// <param name="EngagementCount">How many interactions this channel drew.</param>
internal sealed record CampaignChannelSeedRow(string Name, int PublishedItems, int ReachCount, int EngagementCount);
