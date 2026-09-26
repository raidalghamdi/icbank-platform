namespace Icbank.Platform.Api.Controllers;

/// <summary>The body of <c>POST /campaigns/requests/{id}/review</c>.</summary>
/// <param name="Approve">True to approve and convert into a campaign, false to return it.</param>
/// <param name="Note">The reviewer's note (required when returning).</param>
public sealed record ReviewCampaignRequestBody(bool Approve, string? Note);
