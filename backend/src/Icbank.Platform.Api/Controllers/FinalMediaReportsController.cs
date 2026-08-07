using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Application.MediaMonitoring.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>routes/final-media-reports.ts</c> (API-SURFACE.md §16), the official, numbered,
/// immutable final-report tier. Closes DEFECT-LOG.md SEC-02: the Node source left 7 of these 12
/// routes completely unauthenticated -- generation, manual save, PDF export, email send,
/// exec-summary regeneration, archive search, and the wizard QA-log write. Every mutating and
/// AI/PDF/email-cost route in this controller now requires authentication and the matching
/// <c>media_monitoring:{verb}</c> policy; only the two intentionally-public reads (list/get, per
/// the Node source's own file header comment) remain anonymous. The immutability guard
/// (<see cref="RejectUpdate"/>/<see cref="RejectDelete"/>) always returns 403 regardless of
/// caller identity or role, matching <c>final-media-reports.ts:795-800</c> exactly -- final
/// reports are permanently preserved and can never be edited or deleted through this API.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class FinalMediaReportsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="FinalMediaReportsController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch final-media-report commands/queries.</param>
    public FinalMediaReportsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists final media reports, optionally filtered by type/year. Intentionally public (Node source file header).</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="type">Optional report-type filter.</param>
    /// <param name="year">Optional year filter.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated report list.</returns>
    [HttpGet("final-media-reports")]
    [AllowAnonymous]
    public async Task<ActionResult<PagedResult<FinalMediaReportDto>>> ListAsync(
        [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? type, [FromQuery] int? year, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<FinalMediaReportDto>> result = await _sender.Send(new ListFinalMediaReportsQuery(pagedQuery, type, year), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Fetches a single final media report and increments its view counter. Intentionally public (Node source file header).</summary>
    /// <param name="reportId">The report id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the legacy browser envelope, or 404 if not found.</returns>
    [HttpGet("final-media-reports/{reportId:int}")]
    [AllowAnonymous]
    public async Task<ActionResult> GetByIdAsync(int reportId, CancellationToken cancellationToken)
    {
        Result<FinalMediaReportDetailDto> result = await _sender.Send(new GetFinalMediaReportByIdCommand(reportId), cancellationToken);
        return result.IsSuccess
            ? Ok(new { ok = true, item = ToLegacyBrowserItem(result.Value!) })
            : NotFound(new { error = result.Error });
    }

    /// <summary>Generates the canonical 8-section report draft from cached feed data. Closes SEC-02 (was unauthenticated AI-cost).</summary>
    /// <param name="request">The generation parameters.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the generated draft, or the <c>NO_SOURCE_DATA</c> diagnostic if the range has no source data.</returns>
    [HttpPost("final-media-reports/generate")]
    [Authorize(Policy = "media_monitoring:create")]
    public async Task<ActionResult<GenerateFinalMediaReportResultDto>> GenerateAsync(
        [FromBody] GenerateFinalMediaReportRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new GenerateFinalMediaReportCommand(
            actorUserId,
            request.PeriodLabel,
            request.Audience,
            request.DateFrom,
            request.DateTo,
            request.FocusTopics,
            request.Sources);
        Result<GenerateFinalMediaReportResultDto> result = await _sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
        }

        return result.Value!.NoSourceData is not null
            ? UnprocessableEntity(result.Value!.NoSourceData)
            : Ok(new { draft = result.Value.Draft, saved = (object?)null });
    }

    /// <summary>Manually saves and permanently locks a final report. Closes SEC-02 (Node required <c>requireAdmin</c>; this port requires the equivalent create policy).</summary>
    /// <param name="request">The full report content to persist.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the saved report summary.</returns>
    [HttpPost("final-media-reports")]
    [Authorize(Policy = "media_monitoring:create")]
    public async Task<ActionResult> CreateAsync([FromBody] CreateFinalMediaReportRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new CreateFinalMediaReportCommand(actorUserId, request.Title, request.ReportType, request.PeriodLabel, request.DateFrom, request.DateTo, request.Draft);
        Result<FinalMediaReportDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, new { ok = true, item = result.Value })
            : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Renders a final report to PDF. Closes SEC-02 (was unauthenticated Puppeteer resource-cost endpoint).</summary>
    /// <param name="reportId">The report id to export.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the rendered document bytes, or 404 if not found.</returns>
    [HttpPost("final-media-reports/{reportId:int}/export-pdf")]
    [Authorize(Policy = "media_monitoring:view")]
    public async Task<ActionResult> ExportPdfAsync(int reportId, CancellationToken cancellationToken)
    {
        Result<byte[]> result = await _sender.Send(new ExportFinalMediaReportPdfCommand(reportId), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound(new { error = result.Error });
        }

        return File(result.Value!, "application/pdf");
    }

    /// <summary>Emails a final report to recipients. Closes SEC-02 (was unauthenticated email-send endpoint, an email-cost abuse vector).</summary>
    /// <param name="reportId">The report id to send.</param>
    /// <param name="request">The recipient list and optional subject override.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the send result, or 404 if not found.</returns>
    [HttpPost("final-media-reports/{reportId:int}/send-email")]
    [Authorize(Policy = "media_monitoring:view")]
    public async Task<ActionResult> SendEmailAsync(
        int reportId, [FromBody] SendFinalMediaReportEmailRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new SendFinalMediaReportEmailCommand(actorUserId, reportId, request.Recipients, request.Subject);
        Result<SendFinalMediaReportEmailResultDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(new
            {
                ok = true,
                sent = result.Value!.Sent,
                recipients = result.Value.Recipients,
                note = result.Value.ProviderMessage,
            })
            : NotFound(new { error = result.Error });
    }

    /// <summary>Regenerates just a final report's executive summary without persisting it. Closes SEC-02 (was unauthenticated AI-cost endpoint).</summary>
    /// <param name="reportId">The report id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the regenerated text, or 404 if not found.</returns>
    [HttpPost("final-media-reports/{reportId:int}/exec-summary")]
    [Authorize(Policy = "media_monitoring:edit")]
    public async Task<ActionResult> RegenerateExecutiveSummaryAsync(int reportId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<RegenerateExecutiveSummaryResultDto> result = await _sender.Send(new RegenerateExecutiveSummaryCommand(actorUserId, reportId), cancellationToken);
        return result.IsSuccess
            ? Ok(new { ok = true, summary = result.Value!.Summary, reportNumber = result.Value.ReportNumber })
            : NotFound(new { error = result.Error });
    }

    /// <summary>Searches the final-report archive in full or AI Q&amp;A mode. Closes SEC-02 (was unauthenticated AI-cost endpoint).</summary>
    /// <param name="request">The search mode, query text, and optional limit.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the matched reports (full mode) or AI answer (info mode).</returns>
    [HttpPost("final-media-reports/search")]
    [Authorize(Policy = "media_monitoring:view")]
    public async Task<ActionResult> SearchAsync(
        [FromBody] SearchFinalMediaReportsRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new SearchFinalMediaReportsCommand(actorUserId, request.Mode, request.Query, request.Limit);
        Result<SearchFinalMediaReportsResultDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? Ok(new { ok = true, mode = result.Value!.Mode, reports = result.Value.Reports, answer = result.Value.Answer })
            : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Logs a wizard-answer audit trail entry. Closes SEC-02 (was an unauthenticated write to an audit table).</summary>
    /// <param name="request">The wizard answers to log.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the logged entry id.</returns>
    [HttpPost("qa-queries")]
    [Authorize(Policy = "media_monitoring:view")]
    public async Task<ActionResult> LogWizardQaQueryAsync([FromBody] LogWizardQaQueryRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new LogWizardQaQueryCommand(
            actorUserId, request.Period, request.Audience, request.Sources, request.FocusTopics, request.Language, request.Recipients, request.Mode);
        Result<int> result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, new { ok = true, id = result.Value });
    }

    /// <summary>Seeds 6 demo news items and 6 demo social posts. Closes SEC-02 (Node used a manual inline role check instead of route-level auth).</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the seed counts.</returns>
    [HttpPost("final-media-reports/seed-demo")]
    [Authorize(Policy = "media_monitoring:create")]
    public async Task<ActionResult> SeedDemoNewsAsync(CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<SeedDemoNewsResultDto> result = await _sender.Send(new SeedDemoNewsCommand(actorUserId), cancellationToken);
        return StatusCode(
            StatusCodes.Status201Created,
            new
            {
                ok = true,
                message = result.Value!.Message,
                seededNews = result.Value.SeededNews,
                seededPosts = result.Value.SeededPosts,
            });
    }

    /// <summary>
    /// Immutability guard: final reports can never be edited through this API (BUSINESS-RULES.md
    /// §5.2). Always returns 403 regardless of caller identity or role -- matching
    /// <c>final-media-reports.ts:798-800</c> exactly, which registers no auth middleware at all
    /// on this route, not even a basic authentication check, because the rejection itself is
    /// unconditional and identical for every caller.
    /// </summary>
    /// <param name="reportId">The report id (unused -- rejected before any lookup).</param>
    /// <returns>403 Forbidden.</returns>
    [HttpPut("final-media-reports/{reportId:int}")]
    [AllowAnonymous]
    public ActionResult RejectUpdate(int reportId) =>
        StatusCode(StatusCodes.Status403Forbidden, new { ok = false, error = "التقارير النهائية محفوظة بشكل دائم — لا يمكن تعديلها." });

    /// <summary>
    /// Immutability guard: final reports can never be deleted through this API (BUSINESS-RULES.md
    /// §5.2). Always returns 403 regardless of caller identity or role -- matching
    /// <c>final-media-reports.ts:795-797</c> exactly, which registers no auth middleware at all
    /// on this route for the same reason.
    /// </summary>
    /// <param name="reportId">The report id (unused -- rejected before any lookup).</param>
    /// <returns>403 Forbidden.</returns>
    [HttpDelete("final-media-reports/{reportId:int}")]
    [AllowAnonymous]
    public ActionResult RejectDelete(int reportId) =>
        StatusCode(StatusCodes.Status403Forbidden, new { ok = false, error = "التقارير النهائية محفوظة بشكل دائم — لا يمكن حذفها." });

    private static object ToLegacyBrowserItem(FinalMediaReportDetailDto detail) =>
        new
        {
            detail.Summary.Id,
            detail.Summary.ReportNumber,
            detail.Summary.Title,
            detail.Summary.ReportType,
            detail.Summary.PeriodLabel,
            detail.Summary.DateFrom,
            detail.Summary.DateTo,
            detail.Summary.ExecutiveSummary,
            detail.Summary.Kpis,
            detail.Summary.Status,
            detail.Summary.ViewCount,
            detail.Summary.ContentSha256,
            detail.Summary.CreatedAt,
            detail.TopNews,
            detail.Timeline,
            detail.DigitalPresence,
            detail.EditorialTone,
            detail.DeepAnalysis,
            detail.RegionalComparison,
            detail.Recommendations,
            detail.Alerts,
            detail.QuotesAppendix,
            detail.Methodology,
            detail.Sources,
        };
}
