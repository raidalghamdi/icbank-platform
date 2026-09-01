using Icbank.Platform.Domain.Campaigns;

namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>A single campaign in <see cref="CampaignSeedCatalog"/>.</summary>
/// <param name="Code">The short reference, used as the natural key when seeding.</param>
/// <param name="Name">The campaign name.</param>
/// <param name="Description">The one-line description.</param>
/// <param name="Objective">The communications objective.</param>
/// <param name="Audience">Whether the campaign targets employees or an outside audience.</param>
/// <param name="Status">The lifecycle state.</param>
/// <param name="Owner">The person accountable for the campaign.</param>
/// <param name="Department">The owning organisational unit.</param>
/// <param name="ProgressPercent">The reported completion percentage.</param>
/// <param name="StartOffsetDays">Days from the seed instant to the start date; negative for past dates.</param>
/// <param name="EndOffsetDays">Days from the seed instant to the end date.</param>
/// <param name="LatestUpdate">The latest progress note.</param>
/// <param name="ReachCount">How many people the published material reached.</param>
/// <param name="ImpressionsCount">How many times the published material was displayed.</param>
/// <param name="EngagementCount">How many interactions the published material drew.</param>
/// <param name="PublishedItems">How many pieces of content the campaign has published.</param>
/// <param name="SortOrder">The display order within the audience.</param>
/// <param name="Deliverables">The headline outputs.</param>
/// <param name="Channels">The channels the campaign publishes through.</param>
internal sealed record CampaignSeedRow(
    string Code,
    string Name,
    string Description,
    string Objective,
    CampaignAudience Audience,
    CampaignStatus Status,
    string Owner,
    string Department,
    int ProgressPercent,
    int StartOffsetDays,
    int EndOffsetDays,
    string LatestUpdate,
    int ReachCount,
    int ImpressionsCount,
    int EngagementCount,
    int PublishedItems,
    int SortOrder,
    IReadOnlyList<CampaignDeliverableSeedRow> Deliverables,
    IReadOnlyList<CampaignChannelSeedRow> Channels);
