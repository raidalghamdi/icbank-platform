namespace Icbank.Platform.Application.Campaigns;

/// <summary>
/// The headline figures above a campaigns list. Counted over the campaigns the caller asked for,
/// so the internal page's totals describe internal work only.
/// </summary>
/// <param name="Total">How many campaigns the filter matched.</param>
/// <param name="Running">How many are live.</param>
/// <param name="Upcoming">How many are scheduled but not started.</param>
/// <param name="UnderReview">How many are waiting on a reviewer.</param>
/// <param name="Completed">How many are closed out.</param>
/// <param name="AverageProgressPercent">The mean completion percentage across the matched campaigns.</param>
/// <param name="TotalReach">The summed reach of the matched campaigns.</param>
public sealed record CampaignBoardKpisDto(
    int Total,
    int Running,
    int Upcoming,
    int UnderReview,
    int Completed,
    int AverageProgressPercent,
    int TotalReach);
