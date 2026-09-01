namespace Icbank.Platform.Application.Campaigns;

/// <summary>
/// One tracked campaign, already carrying every value a card or a detail page needs. The schedule
/// position, the output tally and the analytics are computed server-side so the browser renders in
/// a single pass instead of recalculating per campaign.
/// </summary>
/// <param name="Id">The campaign identifier.</param>
/// <param name="Code">The short reference, e.g. <c>INT-01</c>.</param>
/// <param name="Name">The campaign name.</param>
/// <param name="Description">The one-line description.</param>
/// <param name="Objective">The communications objective the campaign is measured against.</param>
/// <param name="Audience">The audience key, <c>internal</c> or <c>external</c>.</param>
/// <param name="AudienceLabel">The Arabic label for the audience.</param>
/// <param name="Status">The lifecycle state key.</param>
/// <param name="StatusLabel">The Arabic label for the lifecycle state.</param>
/// <param name="Owner">The person accountable for the campaign.</param>
/// <param name="Department">The owning organisational unit.</param>
/// <param name="ProgressPercent">The reported completion percentage.</param>
/// <param name="StartDate">The UTC start date.</param>
/// <param name="EndDate">The UTC end date.</param>
/// <param name="DurationDays">The campaign's length in days, inclusive of both ends.</param>
/// <param name="DaysRemaining">Days left until the end date; negative once it has passed.</param>
/// <param name="LatestUpdate">The latest progress note.</param>
/// <param name="DeliverablesCompleted">How many headline outputs are delivered.</param>
/// <param name="DeliverablesTotal">How many headline outputs the campaign has.</param>
/// <param name="Deliverables">The outputs themselves, in display order.</param>
/// <param name="Channels">The channels the campaign publishes through, widest reach first.</param>
/// <param name="Analytics">What the published material achieved.</param>
public sealed record CampaignDto(
    int Id,
    string Code,
    string Name,
    string Description,
    string Objective,
    string Audience,
    string AudienceLabel,
    string Status,
    string StatusLabel,
    string Owner,
    string Department,
    int ProgressPercent,
    DateTime StartDate,
    DateTime EndDate,
    int DurationDays,
    int DaysRemaining,
    string LatestUpdate,
    int DeliverablesCompleted,
    int DeliverablesTotal,
    IReadOnlyList<CampaignDeliverableDto> Deliverables,
    IReadOnlyList<CampaignChannelDto> Channels,
    CampaignAnalyticsDto Analytics);
