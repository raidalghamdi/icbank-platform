namespace Icbank.Platform.Domain.Campaigns;

/// <summary>
/// Who a communications campaign is aimed at. The department runs its internal and external
/// campaigns as two separate books of work with different owners, approval chains and channels,
/// so the audience is the primary split rather than a filter on one shared list.
/// </summary>
public enum CampaignAudience
{
    /// <summary>Aimed at the authority's own employees.</summary>
    Internal = 0,

    /// <summary>Aimed at the public, the media, or the business community.</summary>
    External = 1,
}
