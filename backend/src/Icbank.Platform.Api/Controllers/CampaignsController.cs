using Asp.Versioning;
using Icbank.Platform.Application.Campaigns;
using Icbank.Platform.Application.Campaigns.Queries;
using Icbank.Platform.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Serves the internal and external campaign books. The audience is part of the route rather than
/// a query parameter because the two books are separate RBAC pages — <c>internal_campaigns</c> and
/// <c>external_campaigns</c> — and a single endpoint could not carry both policies. A detail read
/// is gated by its own audience's page and rejects a campaign fetched through the other route, so
/// the route can never be used to read across the permission boundary.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/campaigns")]
public sealed class CampaignsController : ControllerBase
{
    private const string InternalAudience = "internal";
    private const string ExternalAudience = "external";

    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="CampaignsController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch campaign queries.</param>
    public CampaignsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Returns the internal campaign book, optionally narrowed to one lifecycle state.</summary>
    /// <param name="status">The state key to filter on: <c>running</c>, <c>upcoming</c>, <c>under_review</c>, <c>completed</c>, or <c>all</c>.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the campaigns page payload.</returns>
    [HttpGet("internal")]
    [Authorize(Policy = "internal_campaigns:view")]
    public Task<ActionResult<CampaignBoardDto>> GetInternalAsync([FromQuery] string? status, CancellationToken cancellationToken)
        => GetBoardAsync(InternalAudience, status, cancellationToken);

    /// <summary>Returns the external campaign book, optionally narrowed to one lifecycle state.</summary>
    /// <param name="status">The state key to filter on: <c>running</c>, <c>upcoming</c>, <c>under_review</c>, <c>completed</c>, or <c>all</c>.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the campaigns page payload.</returns>
    [HttpGet("external")]
    [Authorize(Policy = "external_campaigns:view")]
    public Task<ActionResult<CampaignBoardDto>> GetExternalAsync([FromQuery] string? status, CancellationToken cancellationToken)
        => GetBoardAsync(ExternalAudience, status, cancellationToken);

    /// <summary>Returns one internal campaign's full detail.</summary>
    /// <param name="campaignId">The campaign to read.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the campaign, or 404 when it is missing, untracked, or not an internal campaign.</returns>
    [HttpGet("internal/{campaignId:int}")]
    [Authorize(Policy = "internal_campaigns:view")]
    public Task<ActionResult<CampaignDto>> GetInternalByIdAsync(int campaignId, CancellationToken cancellationToken)
        => GetByIdAsync(campaignId, InternalAudience, cancellationToken);

    /// <summary>Returns one external campaign's full detail.</summary>
    /// <param name="campaignId">The campaign to read.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the campaign, or 404 when it is missing, untracked, or not an external campaign.</returns>
    [HttpGet("external/{campaignId:int}")]
    [Authorize(Policy = "external_campaigns:view")]
    public Task<ActionResult<CampaignDto>> GetExternalByIdAsync(int campaignId, CancellationToken cancellationToken)
        => GetByIdAsync(campaignId, ExternalAudience, cancellationToken);

    private async Task<ActionResult<CampaignBoardDto>> GetBoardAsync(string audience, string? status, CancellationToken cancellationToken)
    {
        Result<CampaignBoardDto> result = await _sender.Send(new GetCampaignBoardQuery(audience, status), cancellationToken);
        return Ok(result.Value);
    }

    private async Task<ActionResult<CampaignDto>> GetByIdAsync(int campaignId, string audience, CancellationToken cancellationToken)
    {
        Result<CampaignDto> result = await _sender.Send(new GetCampaignByIdQuery(campaignId), cancellationToken);
        CampaignDto? campaign = result.IsSuccess ? result.Value : null;

        // Why: a campaign on the other audience's page is reported as missing rather than
        // forbidden. A caller holding only one of the two pages must not be able to tell an
        // existing campaign on the other page apart from an identifier that was never used.
        if (campaign is null || !string.Equals(campaign.Audience, audience, StringComparison.Ordinal))
        {
            return Problem(GetCampaignByIdQuery.CampaignNotFoundError, statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(campaign);
    }
}
