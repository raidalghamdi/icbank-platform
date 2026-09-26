using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Commands;

/// <summary>
/// Corporate Communications approves or returns an internal campaign request. Approval converts
/// the request straight into an internal campaign in its planning stage, carrying every field over.
/// </summary>
/// <param name="RequestId">The request.</param>
/// <param name="Approve">True to approve and convert, false to return it to the department.</param>
/// <param name="Note">The reviewer's note (required when returning).</param>
/// <param name="ReviewerName">The reviewer's display name.</param>
public sealed record ReviewCampaignRequestCommand(int RequestId, bool Approve, string? Note, string ReviewerName)
    : IRequest<Result<CampaignRequestDto>>
{
    /// <summary>The error returned when the request does not exist.</summary>
    public const string NotFoundError = "الطلب غير موجود.";

    /// <summary>The error returned when the request was already reviewed.</summary>
    public const string AlreadyReviewedError = "تمت مراجعة هذا الطلب مسبقًا.";

    /// <summary>The error returned when a request is returned without a note.</summary>
    public const string NoteRequiredError = "اكتب سبب الإعادة للإدارة الطالبة.";
}
