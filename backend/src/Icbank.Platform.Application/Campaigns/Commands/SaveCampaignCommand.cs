using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Commands;

/// <summary>Creates a campaign (<see cref="CampaignId"/> null) or saves an existing one from the campaign page.</summary>
/// <param name="CampaignId">The campaign to update, or null to create one.</param>
/// <param name="Audience">The audience key: internal or external.</param>
/// <param name="Fields">The campaign fields.</param>
public sealed record SaveCampaignCommand(int? CampaignId, string Audience, SaveCampaignFields Fields) : IRequest<Result<int>>
{
    /// <summary>The error returned when the campaign does not exist on the given audience's page.</summary>
    public const string NotFoundError = "الحملة غير موجودة.";

    /// <summary>The error returned when the name is missing.</summary>
    public const string NameRequiredError = "اسم الحملة مطلوب.";

    /// <summary>The error returned when the period is inverted.</summary>
    public const string PeriodError = "تاريخ النهاية يجب أن يكون بعد تاريخ البداية أو مساويًا له.";

    /// <summary>The error returned for an unknown stage key.</summary>
    public const string StageError = "مرحلة الحملة غير معروفة.";

    /// <summary>The error returned when a campaign is closed without first measuring its performance.</summary>
    public const string ClosureError = "لا يمكن إغلاق الحملة قبل مرحلة قياس الأداء.";

    /// <summary>The error returned when the plan is not a JSON object or is too large.</summary>
    public const string PlanError = "بيانات خطة الحملة غير صالحة.";
}
