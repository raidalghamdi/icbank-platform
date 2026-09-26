using Icbank.Platform.Domain.Campaigns;

namespace Icbank.Platform.Application.Campaigns;

/// <summary>Maps <see cref="CampaignRequest"/> rows to <see cref="CampaignRequestDto"/>.</summary>
public static class CampaignRequestMapper
{
    /// <summary>The support types a department may ask for, with their Arabic labels.</summary>
    public static readonly IReadOnlyDictionary<string, string> SupportLabels = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        ["content"] = "المحتوى",
        ["design"] = "التصميم",
        ["publishing"] = "النشر",
        ["activation"] = "التفعيل",
    };

    /// <summary>Maps one request.</summary>
    /// <param name="request">The request row.</param>
    /// <returns>The DTO.</returns>
    public static CampaignRequestDto ToDto(CampaignRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new CampaignRequestDto(
            request.Id,
            request.RequestingDepartment,
            request.Name,
            request.Objective,
            request.TargetAudience,
            request.ProposedStart,
            request.ProposedEnd,
            request.KeyMessages,
            SplitSupport(request.SupportTypes),
            StatusKey(request.Status),
            StatusLabel(request.Status),
            request.SubmittedByName,
            request.CreatedAt,
            request.ReviewNote,
            request.ReviewedByName,
            request.ReviewedAt,
            request.CampaignId);
    }

    /// <summary>Splits the stored support list, keeping only known keys.</summary>
    /// <param name="supportTypes">The stored comma-separated list.</param>
    /// <returns>The support keys.</returns>
    public static List<string> SplitSupport(string? supportTypes)
        => (supportTypes ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(SupportLabels.ContainsKey)
            .Distinct(StringComparer.Ordinal)
            .ToList();

    private static string StatusKey(CampaignRequestStatus status) => status switch
    {
        CampaignRequestStatus.Approved => "approved",
        CampaignRequestStatus.Rejected => "rejected",
        _ => "submitted",
    };

    private static string StatusLabel(CampaignRequestStatus status) => status switch
    {
        CampaignRequestStatus.Approved => "معتمد — تحوّل إلى حملة",
        CampaignRequestStatus.Rejected => "مُعاد للإدارة",
        _ => "بانتظار مراجعة الاتصال المؤسسي",
    };
}
