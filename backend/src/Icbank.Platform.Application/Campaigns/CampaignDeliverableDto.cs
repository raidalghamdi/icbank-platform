namespace Icbank.Platform.Application.Campaigns;

/// <summary>One headline output of a campaign.</summary>
/// <param name="Title">The output title.</param>
/// <param name="DueDate">The UTC date the output is due.</param>
/// <param name="IsCompleted">Whether the output has been delivered.</param>
public sealed record CampaignDeliverableDto(
    string Title,
    DateTime DueDate,
    bool IsCompleted);
