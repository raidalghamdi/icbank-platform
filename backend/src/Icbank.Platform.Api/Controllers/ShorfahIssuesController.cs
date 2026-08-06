using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Application.Shorfah.Commands;
using Icbank.Platform.Application.Shorfah.Queries;
using Icbank.Platform.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports the Shorfah magazine ISSUE lifecycle and exports (API-SURFACE.md §19, wave 4a scope --
/// section workflow/media/assignments/permissions/SLA/notifications/cron are wave 4b, out of
/// scope here). Every route accepting an issue id runs <see cref="IResourceAuthorizationService.AuthorizeShorfahIssueResourceAsync"/>
/// first (SEC-16): a guessed/stale id fails closed with 404 before any handler logic runs.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shorfah/issues")]
public sealed class ShorfahIssuesController : ControllerBase
{
    private const int DefaultPageSize = 25;

    // Why: mirrors DownloadTokenOptions' own default -- surfaced to callers of the mint endpoint
    // so the frontend can decide how quickly it must start the navigation, without this
    // controller needing to inject IOptions<DownloadTokenOptions> from the Infrastructure layer
    // (R-BE-002: Api may not reach into Infrastructure's option types directly).
    private const int DownloadTokenLifetimeSeconds = 120;

    private readonly ISender _sender;
    private readonly IResourceAuthorizationService _resourceAuthorization;
    private readonly IDownloadTokenService _downloadTokenService;

    /// <summary>Initializes a new instance of the <see cref="ShorfahIssuesController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch Shorfah issue commands/queries.</param>
    /// <param name="resourceAuthorization">The SEC-16 resource-existence port.</param>
    /// <param name="downloadTokenService">The GAP 2 single-use download-token port.</param>
    public ShorfahIssuesController(ISender sender, IResourceAuthorizationService resourceAuthorization, IDownloadTokenService downloadTokenService)
    {
        _sender = sender;
        _resourceAuthorization = resourceAuthorization;
        _downloadTokenService = downloadTokenService;
    }

    /// <summary>Lists all issues, most recent (year/month descending) first.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated issue list.</returns>
    [HttpGet]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> ListAsync([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? DefaultPageSize : pageSize };
        Result<PagedResult<ShorfahIssueDto>> result = await _sender.Send(new ListShorfahIssuesQuery(pagedQuery), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Fetches a single issue with its ordered sections.</summary>
    /// <param name="issueId">The issue being fetched.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{issue, sections}</c>, or 404.</returns>
    [HttpGet("{issueId:int}")]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> GetByIdAsync(int issueId, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        Result<ShorfahIssueDetailDto> result = await _sender.Send(new GetShorfahIssueByIdQuery(issueId), cancellationToken);
        return result.IsSuccess
            ? Ok(new { issue = result.Value!.Issue, sections = result.Value.Sections })
            : NotFound(new { error = result.Error });
    }

    /// <summary>Admin view: sections + assignments + reminders for an issue. Requires the elevated policy in addition to resource existence.</summary>
    /// <param name="issueId">The issue being fetched.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{sections, assignments, reminders}</c>, or 404.</returns>
    [HttpGet("{issueId:int}/admin")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> GetAdminAsync(int issueId, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        Result<ShorfahIssueAdminDto> result = await _sender.Send(new GetShorfahIssueAdminQuery(issueId), cancellationToken);
        return result.IsSuccess
            ? Ok(new { sections = result.Value!.Sections, assignments = result.Value.Assignments, reminders = result.Value.Reminders })
            : NotFound(new { error = result.Error });
    }

    /// <summary>Creates a new issue and auto-seeds the 13 canonical sections.</summary>
    /// <param name="request">The issue metadata.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with <c>{issue}</c>.</returns>
    [HttpPost]
    [Authorize(Policy = "shorfah:create")]
    public async Task<ActionResult> CreateAsync([FromBody] CreateShorfahIssueRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new CreateShorfahIssueCommand(
            actorUserId,
            request.IssueNo,
            request.TitleAr,
            request.SubtitleAr,
            request.Month,
            request.Year,
            request.ContributionsOpenAt,
            request.ContributionsCloseAt,
            request.EditorLetter);
        Result<ShorfahIssueDto> result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { issue = result.Value });
    }

    /// <summary>Edits issue metadata and, optionally, its workflow status.</summary>
    /// <param name="issueId">The issue being edited.</param>
    /// <param name="request">The partial-update payload.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{issue}</c>, or 400/404.</returns>
    [HttpPatch("{issueId:int}")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> UpdateAsync(int issueId, [FromBody] UpdateShorfahIssueRequest request, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new UpdateShorfahIssueCommand(
            actorUserId,
            issueId,
            request.TitleAr,
            request.SubtitleAr,
            request.EditorLetter,
            request.CoverImageUrl,
            request.Status,
            request.ContributionsOpenAt,
            request.ContributionsCloseAt);
        Result<ShorfahIssueDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(new { issue = result.Value }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>
    /// Manually starts collection: seeds the canonical sections if none exist, then sets status
    /// to <c>collecting</c> unless already <c>published</c>. Deliberately gated by the lighter
    /// <c>shorfah:create</c> policy rather than an elevated one -- see AMBIGUOUS-API-3 and
    /// WAVE4A-PORT-NOTES.md §4 for the product sign-off item this preserves.
    /// </summary>
    /// <param name="issueId">The issue being collected.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{issue, sectionsSeeded, sectionsExisting}</c>, or 404.</returns>
    [HttpPost("{issueId:int}/collect")]
    [Authorize(Policy = "shorfah:create")]
    public async Task<ActionResult> CollectAsync(int issueId, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<CollectShorfahIssueResultDto> result = await _sender.Send(new CollectShorfahIssueCommand(actorUserId, issueId), cancellationToken);
        return Ok(new { ok = true, issue = result.Value!.Issue, sectionsSeeded = result.Value.SectionsSeeded, sectionsExisting = result.Value.SectionsExisting });
    }

    /// <summary>Transitions the issue to <c>in_review</c>. Blocked only if already <c>published</c>.</summary>
    /// <param name="issueId">The issue being transitioned.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{issue}</c>, or 400/404.</returns>
    [HttpPost("{issueId:int}/start-review")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> StartReviewAsync(int issueId, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<ShorfahIssueDto> result = await _sender.Send(new StartShorfahIssueReviewCommand(actorUserId, issueId), cancellationToken);
        return result.IsSuccess ? Ok(new { issue = result.Value }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Backfills the 13 canonical sections for an issue that has none.</summary>
    /// <param name="issueId">The issue to backfill sections for.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{ok, sections}</c>, or 400/404.</returns>
    [HttpPost("{issueId:int}/seed-sections")]
    [Authorize(Policy = "shorfah:create")]
    public async Task<ActionResult> SeedSectionsAsync(int issueId, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<int> result = await _sender.Send(new SeedShorfahIssueSectionsCommand(actorUserId, issueId), cancellationToken);
        return result.IsSuccess
            ? Ok(new { ok = true, sections = result.Value })
            : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Adds one custom section to an issue.</summary>
    /// <param name="issueId">The owning issue's id.</param>
    /// <param name="request">The section fields.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with <c>{section}</c>, or 404.</returns>
    [HttpPost("{issueId:int}/sections")]
    [Authorize(Policy = "shorfah:create")]
    public async Task<ActionResult> AddSectionAsync(int issueId, [FromBody] AddShorfahSectionRequest request, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new AddShorfahSectionCommand(
            actorUserId,
            issueId,
            request.SectionType,
            request.TitleAr,
            request.DescriptionAr,
            request.DisplayOrder,
            request.OwnerUserId,
            request.OwnerRole,
            request.AutoGenerate,
            request.GenerationPrompt,
            request.ParentSectionId,
            request.SlaDays);
        Result<ShorfahSectionDto> result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { section = result.Value });
    }

    /// <summary>Starts SLA clocks and notifies every assigned contributor. Rate-limited and audited (cost-abuse vector: fans out real email sends).</summary>
    /// <param name="issueId">The issue whose sections' SLA clocks are being started.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{ok, sent, results}</c>, or 404/429.</returns>
    [HttpPost("{issueId:int}/send-initial")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> SendInitialAsync(int issueId, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<SendShorfahIssueInitialResultDto> result = await _sender.Send(new SendShorfahIssueInitialCommand(actorUserId, issueId), cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(new { ok = true, sent = result.Value!.Sent, results = result.Value.Results });
        }

        var statusCode = result.Error == SendShorfahIssueInitialCommandHandler.RateLimitedError
            ? StatusCodes.Status429TooManyRequests
            : StatusCodes.Status404NotFound;
        return Problem(result.Error, statusCode: statusCode);
    }

    /// <summary>Publishes the issue. Hard precondition: at least one approved+included section.</summary>
    /// <param name="issueId">The issue being published.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{issue}</c>, or 400/404.</returns>
    [HttpPost("{issueId:int}/publish")]
    [Authorize(Policy = "shorfah:edit")]
    public async Task<ActionResult> PublishAsync(int issueId, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<ShorfahIssueDto> result = await _sender.Send(new PublishShorfahIssueCommand(actorUserId, issueId), cancellationToken);
        return result.IsSuccess ? Ok(new { issue = result.Value }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Word (.docx) export of the issue.</summary>
    /// <param name="issueId">The issue being exported.</param>
    /// <param name="preview">When <c>1</c> or <c>true</c>, includes every flagged section regardless of approval.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The rendered document bytes, or 404.</returns>
    [HttpGet("{issueId:int}/docx")]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> ExportDocxAsync(int issueId, [FromQuery] string? preview, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        Result<byte[]> result = await _sender.Send(new GetShorfahIssueDocxQuery(issueId, IsPreview(preview)), cancellationToken);
        return result.IsSuccess
            ? File(result.Value!, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", $"shorfah-issue-{issueId}.docx")
            : NotFound(new { error = result.Error });
    }

    /// <summary>HTML preview of the issue PDF.</summary>
    /// <param name="issueId">The issue being exported.</param>
    /// <param name="preview">When <c>1</c> or <c>true</c>, includes every flagged section regardless of approval.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The rendered HTML, or 404.</returns>
    [HttpGet("{issueId:int}/pdf")]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> GetPdfHtmlAsync(int issueId, [FromQuery] string? preview, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        Result<string> result = await _sender.Send(new GetShorfahIssuePdfHtmlQuery(issueId, IsPreview(preview)), cancellationToken);
        return result.IsSuccess ? Content(result.Value!, "text/html") : NotFound(new { error = result.Error });
    }

    /// <summary>Binary PDF download of the issue.</summary>
    /// <param name="issueId">The issue being exported.</param>
    /// <param name="preview">When <c>1</c> or <c>true</c>, includes every flagged section regardless of approval.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The rendered document bytes, or 404.</returns>
    [HttpGet("{issueId:int}/pdf.pdf")]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> GetPdfBinaryAsync(int issueId, [FromQuery] string? preview, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        Result<byte[]> result = await _sender.Send(new GetShorfahIssuePdfBinaryQuery(issueId, IsPreview(preview)), cancellationToken);
        return result.IsSuccess ? File(result.Value!, "application/pdf") : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Mints a short-lived, single-use download token for this issue's PDF (GAP 2 --
    /// FRONTEND-WIRING-NOTES.md §4: a plain <c>&lt;a href&gt;</c> navigation cannot carry a bearer
    /// header, so the frontend must first call this endpoint with its normal bearer token, then
    /// navigate the browser to the <c>pdf/via-token</c>/<c>pdf.pdf/via-token</c> routes below with
    /// the returned value as a query parameter). Behind the exact same policy and resource check
    /// as the PDF endpoints themselves -- minting proves nothing beyond "this caller could read
    /// this issue a moment ago"; the token-redemption routes re-verify independently.
    /// </summary>
    /// <param name="issueId">The issue the token is scoped to.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{token, expiresInSeconds}</c>, or 404.</returns>
    [HttpPost("{issueId:int}/pdf/download-token")]
    [Authorize(Policy = "shorfah:view")]
    public async Task<ActionResult> IssuePdfDownloadTokenAsync(int issueId, CancellationToken cancellationToken)
    {
        ActionResult? notFound = await EnsureIssueExistsAsync(issueId, cancellationToken);
        if (notFound is not null)
        {
            return notFound;
        }

        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<string> result = await _sender.Send(new IssueShorfahIssueDownloadTokenCommand(actorUserId, issueId), cancellationToken);
        return Ok(new { token = result.Value, expiresInSeconds = DownloadTokenLifetimeSeconds });
    }

    /// <summary>
    /// HTML preview of the issue PDF, reachable by a plain browser navigation via a single-use
    /// token instead of a bearer header (GAP 2). <c>[AllowAnonymous]</c> is safe here specifically
    /// because every other guarantee normally provided by <c>[Authorize]</c> plus
    /// <see cref="EnsureIssueExistsAsync"/> is re-created explicitly inside this action: the token
    /// must exist, be unexpired, be unused, and be scoped to this exact <paramref name="issueId"/>
    /// (checked first, so a stolen/guessed token can never be replayed against a different issue),
    /// and the resource-existence check still runs before any content is served.
    /// </summary>
    /// <param name="issueId">The issue being exported.</param>
    /// <param name="token">The single-use download token minted by <see cref="IssuePdfDownloadTokenAsync"/>.</param>
    /// <param name="preview">When <c>1</c> or <c>true</c>, includes every flagged section regardless of approval.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The rendered HTML, or 401/404.</returns>
    [HttpGet("{issueId:int}/pdf/via-token")]
    [AllowAnonymous]
    public async Task<ActionResult> GetPdfHtmlByTokenAsync(int issueId, [FromQuery] string? token, [FromQuery] string? preview, CancellationToken cancellationToken)
    {
        ActionResult? unauthorizedOrNotFound = await RedeemPdfTokenAsync(issueId, token, cancellationToken);
        if (unauthorizedOrNotFound is not null)
        {
            return unauthorizedOrNotFound;
        }

        Result<string> result = await _sender.Send(new GetShorfahIssuePdfHtmlQuery(issueId, IsPreview(preview)), cancellationToken);
        return result.IsSuccess ? Content(result.Value!, "text/html") : NotFound(new { error = result.Error });
    }

    /// <summary>Binary PDF download of the issue, reachable by a plain browser navigation via a single-use token instead of a bearer header (GAP 2). See <see cref="GetPdfHtmlByTokenAsync"/> for the security reasoning.</summary>
    /// <param name="issueId">The issue being exported.</param>
    /// <param name="token">The single-use download token minted by <see cref="IssuePdfDownloadTokenAsync"/>.</param>
    /// <param name="preview">When <c>1</c> or <c>true</c>, includes every flagged section regardless of approval.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The rendered document bytes, or 401/404.</returns>
    [HttpGet("{issueId:int}/pdf.pdf/via-token")]
    [AllowAnonymous]
    public async Task<ActionResult> GetPdfBinaryByTokenAsync(int issueId, [FromQuery] string? token, [FromQuery] string? preview, CancellationToken cancellationToken)
    {
        ActionResult? unauthorizedOrNotFound = await RedeemPdfTokenAsync(issueId, token, cancellationToken);
        if (unauthorizedOrNotFound is not null)
        {
            return unauthorizedOrNotFound;
        }

        Result<byte[]> result = await _sender.Send(new GetShorfahIssuePdfBinaryQuery(issueId, IsPreview(preview)), cancellationToken);
        return result.IsSuccess ? File(result.Value!, "application/pdf") : NotFound(new { error = result.Error });
    }

    private static bool IsPreview(string? preview) => preview is "1" or "true";

    private async Task<ActionResult?> EnsureIssueExistsAsync(int issueId, CancellationToken cancellationToken)
    {
        ResourceAuthorizationResult authorization = await _resourceAuthorization.AuthorizeShorfahIssueResourceAsync(issueId, cancellationToken);
        return authorization.IsAuthorized ? null : NotFound(new { error = "العدد غير موجود" });
    }

    /// <summary>
    /// Shared token-redemption guard for the two <c>via-token</c> PDF routes: rejects a
    /// missing/wrong/expired/already-used token with 401 before ever touching resource
    /// authorization, then re-runs the exact same <see cref="EnsureIssueExistsAsync"/> resource
    /// check the bearer-only routes use -- a valid token for issue A can never be used to prove
    /// anything about issue B, and a valid token for an issue that has since been deleted still
    /// 404s exactly like the bearer path would.
    /// </summary>
    private async Task<ActionResult?> RedeemPdfTokenAsync(int issueId, string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token) ||
            !await _downloadTokenService.RedeemAsync(token, DownloadResourceType.ShorfahIssuePdf, issueId, cancellationToken))
        {
            return Unauthorized(new { error = "رابط التنزيل غير صالح أو منتهي الصلاحية" });
        }

        return await EnsureIssueExistsAsync(issueId, cancellationToken);
    }
}
