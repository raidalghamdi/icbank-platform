using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Campaigns.Queries;

/// <summary>Reads one campaign's full detail, including every output and channel it carries.</summary>
/// <param name="CampaignId">The campaign to read.</param>
public sealed record GetCampaignByIdQuery(int CampaignId) : IRequest<Result<CampaignDto>>
{
    /// <summary>The error returned when the identifier matches no tracked campaign.</summary>
    public const string CampaignNotFoundError = "الحملة غير موجودة";
}
