namespace Icbank.Platform.Application.Campaigns;

/// <summary>A whole campaigns page payload: one request, no follow-up round trips.</summary>
/// <param name="Kpis">The headline figures, counted before any status filter narrows the list.</param>
/// <param name="Campaigns">The matched campaigns, live first then by sort order.</param>
/// <param name="StatusCounts">How many campaigns sit in each state, so the filter chips can carry counts even while a filter is applied.</param>
/// <param name="GeneratedAt">The UTC instant the payload was computed.</param>
public sealed record CampaignBoardDto(
    CampaignBoardKpisDto Kpis,
    IReadOnlyList<CampaignDto> Campaigns,
    IReadOnlyDictionary<string, int> StatusCounts,
    DateTime GeneratedAt);
