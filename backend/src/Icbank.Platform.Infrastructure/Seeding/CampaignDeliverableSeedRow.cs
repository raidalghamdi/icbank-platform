namespace Icbank.Platform.Infrastructure.Seeding;

/// <summary>A headline output inside a <see cref="CampaignSeedRow"/>.</summary>
/// <param name="Title">The output title.</param>
/// <param name="DueOffsetDays">Days from the seed instant to the output's due date; negative for past dates.</param>
/// <param name="IsCompleted">Whether the output is already delivered.</param>
internal sealed record CampaignDeliverableSeedRow(string Title, int DueOffsetDays, bool IsCompleted);
