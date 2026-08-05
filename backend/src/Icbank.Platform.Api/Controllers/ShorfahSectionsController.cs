using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Application.Shorfah.Commands;
using Icbank.Platform.Application.Shorfah.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports the Shorfah section workflow, media, assignments, permissions, and SLA endpoints
/// (API-SURFACE.md §19, wave 4b scope). Every route accepting a section/media/assignment/permission
/// id runs the matching <see cref="IResourceAuthorizationService"/> check first (SEC-16).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shorfah")]
public sealed class ShorfahSectionsController : ControllerBase
{
    private readonly ISender _sender;
    private readonly IResourceAuthorizationService _resourceAuthorization;

    /// <summary>Initializes a new instance of the <see cref="ShorfahSectionsController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch Shorfah section commands/queries.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-existence port.</param>
    public ShorfahSectionsController(ISender sender, IResourceAuthorizationService resourceAuthorization)
    {
        _sender = sender;
        _resourceAuthorization = resourceAuthorization;
    }

    /// <summary>Edits section content/metadata/SLA with field-level RBAC gating (BUSINESS-RULES.md §1.4).</summary>
    /// <param name="sectionId">The section being edited.</param>
    /// <param name="request">The partial-update payload.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{section}</c>, or 403/404.</returns>
    [HttpPatch("sections/{sectionId:int}")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> PatchSectionAsync(int sectionId, [FromBody] PatchShorfahSectionRequest request, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        var command = new PatchShorfahSectionCommand(
            actorUserId,
            sectionId,
            request.ContentMd,
            request.ContentHtml,
            request.IncludeInPdf,
            request.TitleAr,
            request.DisplayOrder,
            request.DescriptionAr,
            request.SlaDays,
            request.SlaStartsAt,
            request.SlaDeadline);
        Result<ShorfahSectionDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { section = result.Value }) : Forbidden(result.Error!);
    }

    /// <summary>AI auto-generates section content. Admin-only, rate-limited (cost-abuse vector).</summary>
    /// <param name="sectionId">The section being generated.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{section}</c>, or 404/429.</returns>
    [HttpPost("sections/{sectionId:int}/generate")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> GenerateAsync(int sectionId, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        Result<ShorfahSectionDto> result = await _sender.Send(new GenerateShorfahSectionContentCommand(actorUserId, sectionId), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(new { section = result.Value });
        }

        var statusCode = result.Error == GenerateShorfahSectionContentCommandHandler.RateLimitedError
            ? StatusCodes.Status429TooManyRequests
            : StatusCodes.Status400BadRequest;
        return Problem(result.Error, statusCode: statusCode);
    }

    /// <summary>Contributor submits section content.</summary>
    /// <param name="sectionId">The section being submitted.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{section}</c>, or 400/403/404.</returns>
    [HttpPost("sections/{sectionId:int}/submit")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> SubmitAsync(int sectionId, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        Result<ShorfahSectionDto> result = await _sender.Send(new SubmitShorfahSectionCommand(actorUserId, sectionId), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(new { section = result.Value });
        }

        var statusCode = result.Error == SubmitShorfahSectionCommandHandler.ForbiddenError
            ? StatusCodes.Status403Forbidden
            : StatusCodes.Status400BadRequest;
        return Problem(result.Error, statusCode: statusCode);
    }

    /// <summary>Reviewer passes or rejects a section.</summary>
    /// <param name="sectionId">The section being reviewed.</param>
    /// <param name="request">The review decision and optional notes.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{section}</c>, or 403/404.</returns>
    [HttpPost("sections/{sectionId:int}/review")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> ReviewAsync(int sectionId, [FromBody] ReviewShorfahSectionRequest? request, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        var command = new ReviewShorfahSectionCommand(actorUserId, sectionId, request?.Decision, request?.Notes);
        Result<ShorfahSectionDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { section = result.Value }) : Forbidden(result.Error!);
    }

    /// <summary>Approver gives final approval to a section.</summary>
    /// <param name="sectionId">The section being approved.</param>
    /// <param name="request">Optional approval notes.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{section}</c>, or 403/404.</returns>
    [HttpPost("sections/{sectionId:int}/approve")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> ApproveAsync(int sectionId, [FromBody] ApproveShorfahSectionRequest? request, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        Result<ShorfahSectionDto> result = await _sender.Send(new ApproveShorfahSectionCommand(actorUserId, sectionId, request?.Notes), cancellationToken);
        return result.IsSuccess ? Ok(new { section = result.Value }) : Forbidden(result.Error!);
    }

    /// <summary>Assigns a contributor to a section.</summary>
    /// <param name="sectionId">The section being assigned to.</param>
    /// <param name="request">The user and role being assigned.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{assignment}</c>, or 400/404.</returns>
    [HttpPost("sections/{sectionId:int}/assign")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> AssignAsync(int sectionId, [FromBody] AssignShorfahSectionRequest request, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        var command = new AssignShorfahSectionCommand(actorUserId, sectionId, request.UserId, request.Role);
        Result<ShorfahAssignmentDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { assignment = result.Value }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Sends a manual reminder to one assignee.</summary>
    /// <param name="sectionId">The section the reminder concerns.</param>
    /// <param name="request">The single recipient's user id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{ok:true}</c>, or 400/404.</returns>
    [HttpPost("sections/{sectionId:int}/remind")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> RemindAsync(int sectionId, [FromBody] RemindShorfahSectionRequest request, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        Result<bool> result = await _sender.Send(new SendShorfahSectionReminderCommand(actorUserId, sectionId, request.UserId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Lists the workflow audit log for a section, newest first, paginated.</summary>
    /// <param name="sectionId">The section whose log is being read.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated log envelope, or 404.</returns>
    [HttpGet("sections/{sectionId:int}/log")]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> GetLogAsync(int sectionId, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<ShorfahWorkflowLogDto>> result = await _sender.Send(new ListShorfahWorkflowLogQuery(sectionId, pagedQuery), cancellationToken);
        return Ok(new { logs = result.Value!.Items, page = result.Value.Page, pageSize = result.Value.PageSize, total = result.Value.Total });
    }

    /// <summary>Lists media for a section, ordered by display order, paginated. Requires the same view/contribute/review/approve/admin tier as the mutations (closes AMBIGUOUS-API-4 for reads).</summary>
    /// <param name="sectionId">The section whose media is being read.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated media envelope, or 403/404.</returns>
    [HttpGet("sections/{sectionId:int}/media")]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> GetMediaAsync(int sectionId, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<ShorfahSectionMediaDto>> result = await _sender.Send(new ListShorfahSectionMediaQuery(actorUserId, sectionId, pagedQuery), cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error, statusCode: StatusCodes.Status403Forbidden);
        }

        return Ok(new { media = result.Value!.Items, page = result.Value.Page, pageSize = result.Value.PageSize, total = result.Value.Total });
    }

    /// <summary>Uploads media (base64) to a section. 8MB cap; content-type allowlisted.</summary>
    /// <param name="sectionId">The section the media is attached to.</param>
    /// <param name="request">The upload payload.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{media}</c>, or 400/403/404/413.</returns>
    [HttpPost("sections/{sectionId:int}/media")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> UploadMediaAsync(int sectionId, [FromBody] UploadShorfahSectionMediaRequest request, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        var command = new UploadShorfahSectionMediaCommand(actorUserId, sectionId, request.DataBase64, request.ContentType, request.CaptionAr, request.DisplayOrder);
        Result<ShorfahSectionMediaDto> result = await _sender.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(new { media = result.Value });
        }

        var statusCode = result.Error == UploadShorfahSectionMediaCommandHandler.ForbiddenError
            ? StatusCodes.Status403Forbidden
            : result.Error == UploadShorfahSectionMediaCommandHandler.TooLargeError
                ? StatusCodes.Status413PayloadTooLarge
                : StatusCodes.Status400BadRequest;
        return Problem(result.Error, statusCode: statusCode);
    }

    /// <summary>Grants a permission to a user or role on a section.</summary>
    /// <param name="sectionId">The section being granted access to.</param>
    /// <param name="request">The grant target and permission verb.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{permission}</c>, or 400/404.</returns>
    [HttpPost("sections/{sectionId:int}/permissions")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> GrantPermissionAsync(int sectionId, [FromBody] GrantShorfahSectionPermissionRequest request, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        var command = new GrantShorfahSectionPermissionCommand(actorUserId, sectionId, request.UserId, request.RoleName, request.Permission);
        Result<ShorfahSectionPermissionDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { permission = result.Value }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Sets a section's SLA days/start; the deadline is auto-computed.</summary>
    /// <param name="sectionId">The section whose SLA is being set.</param>
    /// <param name="request">The new SLA fields.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{section}</c>, or 404.</returns>
    [HttpPatch("sections/{sectionId:int}/sla")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> UpdateSlaAsync(int sectionId, [FromBody] UpdateShorfahSectionSlaRequest request, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureSectionExistsAsync(sectionId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = RequireActorUserId();
        var command = new UpdateShorfahSectionSlaCommand(actorUserId, sectionId, request.SlaDays, request.SlaStartsAt);
        Result<ShorfahSectionDto> result = await _sender.Send(command, cancellationToken);
        return Ok(new { section = result.Value });
    }

    /// <summary>Removes a section assignment.</summary>
    /// <param name="assignmentId">The assignment being removed.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{ok:true}</c>, or 404.</returns>
    [HttpDelete("assignments/{assignmentId:int}")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> RemoveAssignmentAsync(int assignmentId, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeShorfahAssignmentResourceAsync(assignmentId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return NotFound(new { error = "التكليف غير موجود" });
        }

        var actorUserId = RequireActorUserId();
        Result<bool> result = await _sender.Send(new RemoveShorfahAssignmentCommand(actorUserId, assignmentId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }

    /// <summary>Revokes a section permission grant.</summary>
    /// <param name="permissionId">The permission grant being revoked.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{ok:true}</c>, or 404.</returns>
    [HttpDelete("permissions/{permissionId:int}")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> RevokePermissionAsync(int permissionId, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeShorfahPermissionResourceAsync(permissionId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return NotFound(new { error = "الصلاحية غير موجودة" });
        }

        var actorUserId = RequireActorUserId();
        Result<bool> result = await _sender.Send(new RevokeShorfahSectionPermissionCommand(actorUserId, permissionId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }

    /// <summary>Updates a media row's caption/order. Requires the same contribute/review/approve/admin tier as upload (closes AMBIGUOUS-API-4).</summary>
    /// <param name="mediaId">The media row being edited.</param>
    /// <param name="request">The fields to update.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{media}</c>, or 403/404.</returns>
    [HttpPatch("media/{mediaId:int}")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> PatchMediaAsync(int mediaId, [FromBody] PatchShorfahSectionMediaRequest request, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeShorfahMediaResourceAsync(mediaId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return NotFound(new { error = "الوسائط غير موجودة" });
        }

        var actorUserId = RequireActorUserId();
        var command = new PatchShorfahSectionMediaCommand(actorUserId, mediaId, request.CaptionAr, request.DisplayOrder);
        Result<ShorfahSectionMediaDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { media = result.Value }) : Forbidden(result.Error!);
    }

    /// <summary>Deletes a media row. Requires the same contribute/review/approve/admin tier as upload (closes AMBIGUOUS-API-4).</summary>
    /// <param name="mediaId">The media row being deleted.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{ok:true}</c>, or 403/404.</returns>
    [HttpDelete("media/{mediaId:int}")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> DeleteMediaAsync(int mediaId, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeShorfahMediaResourceAsync(mediaId, cancellationToken);
        if (!authorization.IsAuthorized)
        {
            return NotFound(new { error = "الوسائط غير موجودة" });
        }

        var actorUserId = RequireActorUserId();
        Result<bool> result = await _sender.Send(new DeleteShorfahSectionMediaCommand(actorUserId, mediaId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : Forbidden(result.Error!);
    }

    /// <summary>Lists per-section-type SLA-day defaults.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{defaults}</c>.</returns>
    [HttpGet("sla-defaults")]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> GetSlaDefaultsAsync(CancellationToken cancellationToken)
    {
        Result<IReadOnlyList<ShorfahSlaDefaultDto>> result = await _sender.Send(new ListShorfahSlaDefaultsQuery(), cancellationToken);
        return Ok(new { defaults = result.Value });
    }

    /// <summary>Bulk-updates SLA defaults, optionally propagating to pending/rejected sections.</summary>
    /// <param name="request">The new defaults and propagation flag.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{defaults, propagatedSections}</c>.</returns>
    [HttpPut("sla-defaults")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> UpdateSlaDefaultsAsync([FromBody] UpdateShorfahSlaDefaultsRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = RequireActorUserId();
        var command = new UpdateShorfahSlaDefaultsCommand(actorUserId, request.Defaults, request.Propagate);
        Result<UpdateShorfahSlaDefaultsResultDto> result = await _sender.Send(command, cancellationToken);
        return Ok(new { defaults = result.Value!.Defaults, propagatedSections = result.Value.PropagatedSections });
    }

    private ObjectResult Forbidden(string error) => Problem(error, statusCode: StatusCodes.Status403Forbidden);

    private int RequireActorUserId() =>
        CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");

    private async Task<ActionResult?> EnsureSectionExistsAsync(int sectionId, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeShorfahSectionResourceAsync(sectionId, cancellationToken);
        return authorization.IsAuthorized ? null : NotFound(new { error = "القسم غير موجود" });
    }
}
