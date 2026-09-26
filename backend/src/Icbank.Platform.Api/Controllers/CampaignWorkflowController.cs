using System.Security.Claims;
using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Campaigns;
using Icbank.Platform.Application.Campaigns.Commands;
using Icbank.Platform.Application.Campaigns.Queries;
using Icbank.Platform.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// The operating side of the campaign pages: creating and running campaigns through their
/// lifecycle, attaching files, and the internal campaign request workflow in which a department
/// asks Corporate Communications for a campaign and an approval converts it into one.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/campaigns")]
public sealed class CampaignWorkflowController : ControllerBase
{
    private const int MaxUploadBodyBytes = 21 * 1024 * 1024;
    private const string ReviewPolicy = "internal_campaigns:edit";

    private readonly ISender _sender;
    private readonly IAuthorizationService _authorization;

    /// <summary>Initializes a new instance of the <see cref="CampaignWorkflowController"/> class.</summary>
    /// <param name="sender">The MediatR sender.</param>
    /// <param name="authorization">The authorization service used to tell reviewers apart from requesters.</param>
    public CampaignWorkflowController(ISender sender, IAuthorizationService authorization)
    {
        _sender = sender;
        _authorization = authorization;
    }

    /// <summary>Creates an internal campaign.</summary>
    /// <param name="body">The campaign fields.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created campaign.</returns>
    [HttpPost("internal")]
    [Authorize(Policy = "internal_campaigns:create")]
    public Task<ActionResult> CreateInternalAsync([FromBody] SaveCampaignRequest body, CancellationToken cancellationToken)
        => SaveAsync(null, "internal", body, cancellationToken);

    /// <summary>Creates an external campaign.</summary>
    /// <param name="body">The campaign fields.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The created campaign.</returns>
    [HttpPost("external")]
    [Authorize(Policy = "external_campaigns:create")]
    public Task<ActionResult> CreateExternalAsync([FromBody] SaveCampaignRequest body, CancellationToken cancellationToken)
        => SaveAsync(null, "external", body, cancellationToken);

    /// <summary>Saves an internal campaign.</summary>
    /// <param name="campaignId">The campaign.</param>
    /// <param name="body">The campaign fields.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The saved campaign.</returns>
    [HttpPut("internal/{campaignId:int}")]
    [Authorize(Policy = "internal_campaigns:edit")]
    public Task<ActionResult> SaveInternalAsync(int campaignId, [FromBody] SaveCampaignRequest body, CancellationToken cancellationToken)
        => SaveAsync(campaignId, "internal", body, cancellationToken);

    /// <summary>Saves an external campaign.</summary>
    /// <param name="campaignId">The campaign.</param>
    /// <param name="body">The campaign fields.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The saved campaign.</returns>
    [HttpPut("external/{campaignId:int}")]
    [Authorize(Policy = "external_campaigns:edit")]
    public Task<ActionResult> SaveExternalAsync(int campaignId, [FromBody] SaveCampaignRequest body, CancellationToken cancellationToken)
        => SaveAsync(campaignId, "external", body, cancellationToken);

    /// <summary>Attaches a file to an internal campaign.</summary>
    /// <param name="campaignId">The campaign.</param>
    /// <param name="body">The file.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The stored file.</returns>
    [HttpPost("internal/{campaignId:int}/files")]
    [Authorize(Policy = "internal_campaigns:edit")]
    [RequestSizeLimit(MaxUploadBodyBytes)]
    public Task<ActionResult> UploadInternalAsync(int campaignId, [FromBody] UploadCampaignFileBody body, CancellationToken cancellationToken)
        => UploadAsync(campaignId, body, cancellationToken);

    /// <summary>Attaches a file to an external campaign.</summary>
    /// <param name="campaignId">The campaign.</param>
    /// <param name="body">The file.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The stored file.</returns>
    [HttpPost("external/{campaignId:int}/files")]
    [Authorize(Policy = "external_campaigns:edit")]
    [RequestSizeLimit(MaxUploadBodyBytes)]
    public Task<ActionResult> UploadExternalAsync(int campaignId, [FromBody] UploadCampaignFileBody body, CancellationToken cancellationToken)
        => UploadAsync(campaignId, body, cancellationToken);

    /// <summary>Lists internal campaign requests: all of them for reviewers, the caller's own otherwise.</summary>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The requests and whether the caller can review them.</returns>
    [HttpGet("requests")]
    [Authorize]
    public async Task<ActionResult> ListRequestsAsync(CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var canReview = (await _authorization.AuthorizeAsync(User, ReviewPolicy)).Succeeded;
        Result<IReadOnlyList<CampaignRequestDto>> result = await _sender.Send(new ListCampaignRequestsQuery(actorUserId, canReview), cancellationToken);
        return Ok(new { items = result.Value, canReview });
    }

    /// <summary>Submits an internal campaign request to Corporate Communications. Open to every signed-in employee.</summary>
    /// <param name="body">The request.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The submitted request.</returns>
    [HttpPost("requests")]
    [Authorize]
    public async Task<ActionResult> SubmitRequestAsync([FromBody] SubmitCampaignRequestBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new SubmitCampaignRequestCommand(
            actorUserId,
            User.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
            body.RequestingDepartment,
            body.Name,
            body.Objective,
            body.TargetAudience,
            body.ProposedStart,
            body.ProposedEnd,
            body.KeyMessages ?? string.Empty,
            body.SupportTypes ?? Array.Empty<string>());
        Result<CampaignRequestDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }

    /// <summary>Approves (and converts) or returns an internal campaign request.</summary>
    /// <param name="requestId">The request.</param>
    /// <param name="body">The decision.</param>
    /// <param name="cancellationToken">The request cancellation token.</param>
    /// <returns>The reviewed request.</returns>
    [HttpPost("requests/{requestId:int}/review")]
    [Authorize(Policy = ReviewPolicy)]
    public async Task<ActionResult> ReviewRequestAsync(int requestId, [FromBody] ReviewCampaignRequestBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var reviewer = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
        Result<CampaignRequestDto> result = await _sender.Send(new ReviewCampaignRequestCommand(requestId, body.Approve, body.Note, reviewer), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        return result.Error == ReviewCampaignRequestCommand.NotFoundError
            ? NotFound(new { error = result.Error })
            : BadRequest(new { error = result.Error });
    }

    private async Task<ActionResult> SaveAsync(int? campaignId, string audience, SaveCampaignRequest body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        var planJson = body.Plan is { ValueKind: System.Text.Json.JsonValueKind.Object } plan ? plan.GetRawText() : null;
        var fields = new SaveCampaignFields(
            body.Name,
            body.Objective,
            body.TargetAudience,
            body.StartDate,
            body.EndDate,
            body.KeyMessages,
            body.Owner,
            body.TeamMembers,
            body.PlannedChannels,
            body.Stage,
            planJson,
            body.LatestUpdate);
        Result<int> saved = await _sender.Send(new SaveCampaignCommand(campaignId, audience, fields), cancellationToken);
        if (!saved.IsSuccess)
        {
            return saved.Error == SaveCampaignCommand.NotFoundError
                ? NotFound(new { error = saved.Error })
                : BadRequest(new { error = saved.Error });
        }

        Result<CampaignDto> campaign = await _sender.Send(new GetCampaignByIdQuery(saved.Value), cancellationToken);
        return Ok(campaign.Value);
    }

    private async Task<ActionResult> UploadAsync(int campaignId, UploadCampaignFileBody body, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(body);
        Result<CampaignFileDto> result = await _sender.Send(
            new UploadCampaignFileCommand(campaignId, body.FileName, body.ContentType, body.DataBase64), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : BadRequest(new { error = result.Error });
    }
}
