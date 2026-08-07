using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Weekend;
using Icbank.Platform.Application.Weekend.Commands;
using Icbank.Platform.Application.Weekend.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>routes/weekend-drafts.ts</c> (API-SURFACE.md §10). Every admin route uses the
/// centralized <c>weekend:{verb}</c> policies instead of the Node source's blanket
/// <c>requireAdmin</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/weekend")]
public sealed class WeekendDraftsController : ControllerBase
{
    private const int DefaultPageSize = 25;

    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="WeekendDraftsController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch weekend-draft commands/queries.</param>
    public WeekendDraftsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Fetches the latest published draft for a target weekend, falling back to the most recent published draft overall.</summary>
    /// <param name="date">The requested target date, or omit to default to the next Riyadh Thursday.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{draft}</c> (possibly <c>null</c>).</returns>
    [HttpGet("published")]
    [Authorize]
    public async Task<ActionResult> GetPublishedAsync([FromQuery] string? date, CancellationToken cancellationToken)
    {
        Result<WeekendDraftDto?> result = await _sender.Send(new GetPublishedWeekendDraftQuery(date), cancellationToken);
        return Ok(new { draft = result.Value });
    }

    /// <summary>Lists drafts, optionally filtered by status.</summary>
    /// <param name="status">Optional exact-match status filter.</param>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{drafts}</c> paginated.</returns>
    [HttpGet("drafts")]
    [Authorize(Policy = "weekend:view")]
    public async Task<ActionResult> ListDraftsAsync([FromQuery] string? status, [FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? DefaultPageSize : pageSize };
        Result<PagedResult<WeekendDraftDto>> result = await _sender.Send(new ListWeekendDraftsQuery(pagedQuery, status), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Fetches a single draft by id.</summary>
    /// <param name="draftId">The draft being fetched.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{draft}</c>, or 404.</returns>
    [HttpGet("drafts/{draftId:int}")]
    [Authorize(Policy = "weekend:view")]
    public async Task<ActionResult> GetByIdAsync(int draftId, CancellationToken cancellationToken)
    {
        Result<WeekendDraftDto> result = await _sender.Send(new GetWeekendDraftByIdQuery(draftId), cancellationToken);
        return result.IsSuccess ? Ok(new { draft = result.Value }) : NotFound(new { error = result.Error });
    }

    /// <summary>Generates a new AI weekend content bundle.</summary>
    /// <param name="request">The optional target weekend date override.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with <c>{draft}</c>.</returns>
    [HttpPost("generate")]
    [Authorize(Policy = "weekend:create")]
    public async Task<ActionResult> GenerateAsync([FromBody] GenerateWeekendDraftRequest? request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<WeekendDraftDto> result = await _sender.Send(new GenerateWeekendDraftCommand(actorUserId, request?.WeekendDate), cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { draft = result.Value });
    }

    /// <summary>Approves a <c>pending_review</c> draft.</summary>
    /// <param name="draftId">The draft being approved.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{draft}</c>, or 400/404.</returns>
    [HttpPost("drafts/{draftId:int}/approve")]
    [Authorize(Policy = "weekend:edit")]
    public async Task<ActionResult> ApproveAsync(int draftId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<WeekendDraftDto> result = await _sender.Send(new ApproveWeekendDraftCommand(actorUserId, draftId), cancellationToken);
        return result.IsSuccess
            ? Ok(new { draft = result.Value })
            : Problem(result.Error, statusCode: result.Error == "المسودة غير موجودة" ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
    }

    /// <summary>Publishes an <c>approved</c> or <c>pending_review</c> draft.</summary>
    /// <param name="draftId">The draft being published.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{draft}</c>, or 400/404.</returns>
    [HttpPost("drafts/{draftId:int}/publish")]
    [Authorize(Policy = "weekend:edit")]
    public async Task<ActionResult> PublishAsync(int draftId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<WeekendDraftDto> result = await _sender.Send(new PublishWeekendDraftCommand(actorUserId, draftId), cancellationToken);
        return result.IsSuccess
            ? Ok(new { draft = result.Value })
            : Problem(result.Error, statusCode: result.Error == "المسودة غير موجودة" ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
    }

    /// <summary>Rejects a draft. No status precondition — can reject from any state.</summary>
    /// <param name="draftId">The draft being rejected.</param>
    /// <param name="request">The optional rejection reason.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{draft}</c>, or 404.</returns>
    [HttpPost("drafts/{draftId:int}/reject")]
    [Authorize(Policy = "weekend:edit")]
    public async Task<ActionResult> RejectAsync(int draftId, [FromBody] RejectWeekendDraftRequest? request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<WeekendDraftDto> result = await _sender.Send(new RejectWeekendDraftCommand(actorUserId, draftId, request?.Reason), cancellationToken);
        return result.IsSuccess ? Ok(new { draft = result.Value }) : NotFound(new { error = result.Error });
    }

    /// <summary>Manually edits a draft's content before approval.</summary>
    /// <param name="draftId">The draft being edited.</param>
    /// <param name="request">The replacement content payload.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{draft}</c>, or 400/404.</returns>
    [HttpPatch("drafts/{draftId:int}")]
    [Authorize(Policy = "weekend:edit")]
    public async Task<ActionResult> EditContentAsync(int draftId, [FromBody] EditWeekendDraftContentRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new EditWeekendDraftContentCommand(actorUserId, draftId, request.Content.GetRawText());
        Result<WeekendDraftDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(new { draft = result.Value })
            : Problem(result.Error, statusCode: result.Error == "المسودة غير موجودة" ? StatusCodes.Status404NotFound : StatusCodes.Status400BadRequest);
    }

    /// <summary>"Sends" the weekend report via one or more channels. Honestly reports every channel as not-yet-implemented (closes BUG-01).</summary>
    /// <param name="request">The requested channels, provider, and period.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the honest per-channel results, or 400 if no channels were supplied.</returns>
    [HttpPost("send")]
    [Authorize(Policy = "weekend:create")]
    public async Task<ActionResult> SendAsync([FromBody] SendWeekendReportRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var channels = request.Channels.Select(c => new WeekendReportChannel(c.Type, c.To, c.Kind)).ToList();
        var command = new SendWeekendReportCommand(actorUserId, channels, request.Provider ?? "unifonic", request.Period ?? "weekend");
        Result<SendWeekendReportResultDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Deletes a draft.</summary>
    /// <param name="draftId">The draft being deleted.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404.</returns>
    [HttpDelete("drafts/{draftId:int}")]
    [Authorize(Policy = "weekend:delete")]
    public async Task<ActionResult> DeleteAsync(int draftId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new DeleteWeekendDraftCommand(actorUserId, draftId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }
}
