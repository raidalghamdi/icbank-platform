namespace Icbank.Platform.Application.Campaigns;

/// <summary>An internal campaign request as shown to the department and to Corporate Communications.</summary>
/// <param name="Id">The request id.</param>
/// <param name="RequestingDepartment">The requesting department.</param>
/// <param name="Name">The proposed campaign name.</param>
/// <param name="Objective">The objective.</param>
/// <param name="TargetAudience">The target audience.</param>
/// <param name="ProposedStart">The first day of the proposed period.</param>
/// <param name="ProposedEnd">The last day of the proposed period.</param>
/// <param name="KeyMessages">The key messages, one per line.</param>
/// <param name="SupportTypes">The required support keys.</param>
/// <param name="Status">The review state key: submitted, approved or rejected.</param>
/// <param name="StatusLabel">The Arabic review state label.</param>
/// <param name="SubmittedByName">Who submitted the request.</param>
/// <param name="SubmittedAt">When it was submitted (UTC).</param>
/// <param name="ReviewNote">The reviewer's note.</param>
/// <param name="ReviewedByName">Who reviewed it.</param>
/// <param name="ReviewedAt">When it was reviewed (UTC).</param>
/// <param name="CampaignId">The campaign it was converted into.</param>
public sealed record CampaignRequestDto(
    int Id,
    string RequestingDepartment,
    string Name,
    string Objective,
    string TargetAudience,
    DateTime ProposedStart,
    DateTime ProposedEnd,
    string KeyMessages,
    IReadOnlyList<string> SupportTypes,
    string Status,
    string StatusLabel,
    string SubmittedByName,
    DateTime SubmittedAt,
    string ReviewNote,
    string ReviewedByName,
    DateTime? ReviewedAt,
    int? CampaignId);
