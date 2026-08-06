using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.InternationalDays;
using Icbank.Platform.Application.InternationalDays.Commands;
using Icbank.Platform.Application.InternationalDays.Queries;
using Icbank.Platform.Domain.Identity;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>routes/international-days.ts</c> (API-SURFACE.md §14), gated by the
/// <c>international_days:{verb}</c> policy family (already seeded -- <c>PageSlugs.InternationalDays</c>).
/// The Node source's dual-provider "merge" logic (BUSINESS-RULES.md §4.3) is deliberately NOT
/// ported since it was dead code in the live search path (DEFECT-LOG.md ARCH-07); see
/// WAVE2-PORT-NOTES.md. Closes SEC-21/H-1 (unescaped AI content in export) and DATA-04/H-2
/// (unvalidated AI JSON persisted) -- see <see cref="InternationalDayHtmlExportBuilder"/> and
/// <see cref="DaySearchResultValidator"/> respectively.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/intl-days")]
public sealed class InternationalDaysController : ControllerBase
{
    // Why: mirrors DownloadTokenOptions' own default -- see the identical constant/comment on
    // ShorfahIssuesController for why this stays a local literal rather than an injected options
    // read (R-BE-002: Api may not reach into Infrastructure's option types directly).
    private const int DownloadTokenLifetimeSeconds = 120;

    private readonly ISender _sender;
    private readonly IDownloadTokenService _downloadTokenService;

    /// <summary>Initializes a new instance of the <see cref="InternationalDaysController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch international-days commands/queries.</param>
    /// <param name="downloadTokenService">The GAP 2 single-use download-token port.</param>
    public InternationalDaysController(ISender sender, IDownloadTokenService downloadTokenService)
    {
        _sender = sender;
        _downloadTokenService = downloadTokenService;
    }

    /// <summary>Runs (or serves from cache) an AI research search for a day name.</summary>
    /// <param name="request">The search request.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the search result, or 429/400 on rate-limit/validation failure.</returns>
    [HttpPost("search")]
    [Authorize(Policy = "international_days:view")]
    public async Task<ActionResult<SearchInternationalDayResultDto>> SearchAsync(
        [FromBody] SearchInternationalDayRequest request, CancellationToken cancellationToken)
    {
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        var command = new SearchInternationalDayCommand(request.Query, request.Category, request.ForceRefresh, ipAddress);
        Result<SearchInternationalDayResultDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status429TooManyRequests);
    }

    /// <summary>Persists a search result across the day, theme, activations, design samples, and sources.</summary>
    /// <param name="request">The search result to persist.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the saved day, or 400 if the AI result fails schema validation.</returns>
    [HttpPost("save")]
    [Authorize(Policy = "international_days:create")]
    public async Task<ActionResult<SaveInternationalDayResultDto>> SaveAsync(
        [FromBody] SaveInternationalDayRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<SaveInternationalDayResultDto> result =
            await _sender.Send(new SaveInternationalDayCommand(actorUserId, request.Data, request.Category), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Lists all recorded days with themes and activation counts.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="q">Optional fuzzy search text.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated archive list.</returns>
    [HttpGet("archive")]
    [Authorize(Policy = "international_days:view")]
    public async Task<ActionResult<PagedResult<InternationalDayArchiveItemDto>>> ListArchiveAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] string? q,
        [FromQuery] string? category,
        [FromQuery] int? year,
        CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<InternationalDayArchiveItemDto>> result =
            await _sender.Send(new ListInternationalDaysArchiveQuery(pagedQuery, q, category, year), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Fetches a single day with its themes, activations, and sources.</summary>
    /// <param name="dayId">The day id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the day detail, or 404 if not found.</returns>
    [HttpGet("{dayId:int}")]
    [Authorize(Policy = "international_days:view")]
    public async Task<ActionResult<InternationalDayDetailDto>> GetByIdAsync(int dayId, CancellationToken cancellationToken)
    {
        Result<InternationalDayDetailDto> result = await _sender.Send(new GetInternationalDayByIdQuery(dayId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Deletes a day (cascades to its themes/activations/sources via real FK constraints).</summary>
    /// <param name="dayId">The day id to delete.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpDelete("{dayId:int}")]
    [Authorize(Policy = "international_days:delete")]
    public async Task<ActionResult> DeleteAsync(int dayId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new DeleteInternationalDayCommand(actorUserId, dayId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }

    /// <summary>Exports a day as a Word-compatible, fully HTML-encoded document (closes SEC-21/H-1).</summary>
    /// <param name="dayId">The day id to export.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The exported document, or 404 if not found.</returns>
    [HttpGet("export/{dayId:int}")]
    [Authorize(Policy = "international_days:view")]
    public async Task<IActionResult> ExportAsync(int dayId, CancellationToken cancellationToken)
    {
        Result<InternationalDayHtmlExportDto> result = await _sender.Send(new ExportInternationalDayHtmlQuery(dayId), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        var bytes = System.Text.Encoding.UTF8.GetBytes(result.Value!.Html);
        var fileName = Uri.EscapeDataString(result.Value.FileNameWithoutExtension) + ".doc";
        return File(bytes, "application/vnd.ms-word; charset=utf-8", fileName);
    }

    /// <summary>
    /// Mints a short-lived, single-use download token for this day's export (GAP 2 --
    /// FRONTEND-WIRING-NOTES.md §4: <c>idExport()</c> uses <c>window.open(...)</c>, a plain
    /// browser navigation that cannot carry a bearer header). Behind the same policy as the export
    /// endpoint itself, and independently re-checks the day still exists before minting.
    /// </summary>
    /// <param name="dayId">The day the token is scoped to.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{token, expiresInSeconds}</c>, or 404.</returns>
    [HttpPost("export/{dayId:int}/download-token")]
    [Authorize(Policy = "international_days:view")]
    public async Task<ActionResult> IssueExportDownloadTokenAsync(int dayId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<string> result = await _sender.Send(new IssueInternationalDayDownloadTokenCommand(actorUserId, dayId), cancellationToken);
        return result.IsSuccess
            ? Ok(new { token = result.Value, expiresInSeconds = DownloadTokenLifetimeSeconds })
            : NotFound(new { error = result.Error });
    }

    /// <summary>
    /// Exports a day, reachable by a plain browser navigation via a single-use token instead of a
    /// bearer header (GAP 2). <c>[AllowAnonymous]</c> is safe here for the same reason it is safe
    /// on <c>ShorfahIssuesController</c>'s <c>via-token</c> routes: the token must exist, be
    /// unexpired, be unused, and be scoped to this exact <paramref name="dayId"/>, checked before
    /// any content is served, and a day that no longer exists still 404s exactly like the bearer
    /// path (<see cref="ExportAsync"/>) would.
    /// </summary>
    /// <param name="dayId">The day id to export.</param>
    /// <param name="token">The single-use download token minted by <see cref="IssueExportDownloadTokenAsync"/>.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The exported document, or 401/404.</returns>
    [HttpGet("export/{dayId:int}/via-token")]
    [AllowAnonymous]
    public async Task<IActionResult> ExportByTokenAsync(int dayId, [FromQuery] string? token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(token) ||
            !await _downloadTokenService.RedeemAsync(token, DownloadResourceType.InternationalDayExport, dayId, cancellationToken))
        {
            return Unauthorized(new { error = "رابط التنزيل غير صالح أو منتهي الصلاحية" });
        }

        return await ExportAsync(dayId, cancellationToken);
    }
}
