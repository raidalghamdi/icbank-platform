namespace Icbank.Platform.Application.Campaigns;

/// <summary>
/// What the campaign's published material achieved. The engagement rate is resolved here rather
/// than in the browser so every surface that shows the campaign quotes the same figure.
/// </summary>
/// <param name="ReachCount">How many people the published material reached.</param>
/// <param name="ImpressionsCount">How many times the published material was displayed.</param>
/// <param name="EngagementCount">How many interactions the published material drew.</param>
/// <param name="PublishedItems">How many pieces of content the campaign has published.</param>
/// <param name="EngagementRatePerMille">Interactions per thousand impressions, so a sub-percent rate still reads as a whole number.</param>
public sealed record CampaignAnalyticsDto(
    int ReachCount,
    int ImpressionsCount,
    int EngagementCount,
    int PublishedItems,
    int EngagementRatePerMille);
