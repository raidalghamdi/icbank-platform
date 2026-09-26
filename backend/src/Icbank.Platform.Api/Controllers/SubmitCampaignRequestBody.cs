namespace Icbank.Platform.Api.Controllers;

/// <summary>The body of <c>POST /campaigns/requests</c>.</summary>
/// <param name="RequestingDepartment">The requesting department.</param>
/// <param name="Name">The proposed campaign name.</param>
/// <param name="Objective">The objective.</param>
/// <param name="TargetAudience">The target audience.</param>
/// <param name="ProposedStart">The first day of the proposed period.</param>
/// <param name="ProposedEnd">The last day of the proposed period.</param>
/// <param name="KeyMessages">The key messages, one per line.</param>
/// <param name="SupportTypes">The required support keys.</param>
public sealed record SubmitCampaignRequestBody(
    string RequestingDepartment,
    string Name,
    string Objective,
    string TargetAudience,
    DateTime ProposedStart,
    DateTime ProposedEnd,
    string? KeyMessages,
    IReadOnlyList<string>? SupportTypes);
