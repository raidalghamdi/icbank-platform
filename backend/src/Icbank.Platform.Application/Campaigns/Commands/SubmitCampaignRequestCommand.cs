using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Commands;

/// <summary>A department submits an internal campaign request to Corporate Communications.</summary>
/// <param name="ActorUserId">The submitting employee.</param>
/// <param name="ActorName">The submitting employee's display name.</param>
/// <param name="RequestingDepartment">The requesting department.</param>
/// <param name="Name">The proposed campaign name.</param>
/// <param name="Objective">The objective.</param>
/// <param name="TargetAudience">The target audience.</param>
/// <param name="ProposedStart">The first day of the proposed period.</param>
/// <param name="ProposedEnd">The last day of the proposed period.</param>
/// <param name="KeyMessages">The key messages, one per line.</param>
/// <param name="SupportTypes">The required support keys (content, design, publishing, activation).</param>
public sealed record SubmitCampaignRequestCommand(
    int ActorUserId,
    string ActorName,
    string RequestingDepartment,
    string Name,
    string Objective,
    string TargetAudience,
    DateTime ProposedStart,
    DateTime ProposedEnd,
    string KeyMessages,
    IReadOnlyList<string> SupportTypes) : IRequest<Result<CampaignRequestDto>>
{
    /// <summary>The error returned when a required field is missing.</summary>
    public const string RequiredError = "الإدارة الطالبة واسم الحملة والهدف والجمهور المستهدف حقول مطلوبة.";

    /// <summary>The error returned when no support type is selected.</summary>
    public const string SupportError = "اختر نوع دعم واحدًا على الأقل.";
}
