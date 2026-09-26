namespace Icbank.Platform.Application.Campaigns.Commands;

/// <summary>The editable fields of a campaign.</summary>
/// <param name="Name">The campaign name.</param>
/// <param name="Objective">The objective.</param>
/// <param name="TargetAudience">The target audience.</param>
/// <param name="StartDate">The first day.</param>
/// <param name="EndDate">The last day.</param>
/// <param name="KeyMessages">The key messages, one per line.</param>
/// <param name="Owner">The campaign owner.</param>
/// <param name="TeamMembers">The team members, one per line.</param>
/// <param name="PlannedChannels">The publishing / activation channels, one per line.</param>
/// <param name="Stage">The lifecycle stage key.</param>
/// <param name="PlanJson">The operating plan document as a JSON object.</param>
/// <param name="LatestUpdate">An optional short status note.</param>
public sealed record SaveCampaignFields(
    string Name,
    string? Objective,
    string? TargetAudience,
    DateTime StartDate,
    DateTime EndDate,
    string? KeyMessages,
    string? Owner,
    string? TeamMembers,
    string? PlannedChannels,
    string Stage,
    string? PlanJson,
    string? LatestUpdate);
