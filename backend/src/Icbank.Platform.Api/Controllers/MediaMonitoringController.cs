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
/// Ports <c>routes/media-monitoring.ts</c> (API-SURFACE.md §15), gated by the seeded
/// <c>media_monitoring:{verb}</c> policy family. Closes DEFECT-LOG.md SEC-02: the Node source
/// left 5 of these 11 routes completely unauthenticated (report generation, both prompt-library
/// writes, prompt execution, and the AI Quick tool) -- every mutating and AI-cost route in this
/// controller now requires authentication and the matching page/verb policy.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class MediaMonitoringController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="MediaMonitoringController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch media-monitoring commands/queries.</param>
    public MediaMonitoringController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists published media-monitoring reports.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="audience">Optional audience-tier filter.</param>
    /// <param name="reportType">Optional report-type filter.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated report list.</returns>
    [HttpGet("media-reports")]
    [Authorize(Policy = "media_monitoring:view")]
    public async Task<ActionResult<PagedResult<MediaReportDto>>> ListReportsAsync(
        [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? audience, [FromQuery] string? reportType, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<MediaReportDto>> result = await _sender.Send(new ListMediaReportsQuery(pagedQuery, audience, reportType), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Fetches a single media-monitoring report.</summary>
    /// <param name="reportId">The report id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpGet("media-reports/{reportId:int}")]
    [Authorize(Policy = "media_monitoring:view")]
    public async Task<ActionResult<MediaReportDto>> GetReportByIdAsync(int reportId, CancellationToken cancellationToken)
    {
        Result<MediaReportDto> result = await _sender.Send(new GetMediaReportByIdQuery(reportId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Generates a new audience-tiered media-monitoring report from cached feed data. Closes SEC-02 (was unauthenticated).</summary>
    /// <param name="request">The generation parameters.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the generated report.</returns>
    [HttpPost("media-reports/generate")]
    [Authorize(Policy = "media_monitoring:create")]
    public async Task<ActionResult<MediaReportDto>> GenerateReportAsync([FromBody] GenerateMediaReportRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new GenerateMediaReportCommand(
            actorUserId, request.Audience, request.ReportType, request.DateFrom, request.DateTo, request.Sources, request.CustomTitle);
        Result<MediaReportDto> result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>Deletes a media-monitoring report.</summary>
    /// <param name="reportId">The report id to delete.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpDelete("media-reports/{reportId:int}")]
    [Authorize(Policy = "media_monitoring:delete")]
    public async Task<ActionResult> DeleteReportAsync(int reportId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new DeleteMediaReportCommand(actorUserId, reportId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }

    /// <summary>Lists active prompt frameworks.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="kind">Optional kind filter.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated framework list.</returns>
    [HttpGet("prompts")]
    [Authorize(Policy = "media_monitoring:view")]
    public async Task<ActionResult<PagedResult<PromptFrameworkDto>>> ListPromptsAsync(
        [FromQuery] int page, [FromQuery] int pageSize, [FromQuery] string? category, [FromQuery] string? kind, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<PromptFrameworkDto>> result = await _sender.Send(new ListPromptFrameworksQuery(pagedQuery, category, kind), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Fetches a single prompt framework.</summary>
    /// <param name="promptId">The framework id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpGet("prompts/{promptId:int}")]
    [Authorize(Policy = "media_monitoring:view")]
    public async Task<ActionResult<PromptFrameworkDto>> GetPromptByIdAsync(int promptId, CancellationToken cancellationToken)
    {
        Result<PromptFrameworkDto> result = await _sender.Send(new GetPromptFrameworkByIdQuery(promptId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Creates a prompt framework. Closes SEC-02 (was unauthenticated).</summary>
    /// <param name="request">The new framework's fields.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the new framework.</returns>
    [HttpPost("prompts")]
    [Authorize(Policy = "media_monitoring:create")]
    public async Task<ActionResult<PromptFrameworkDto>> CreatePromptAsync([FromBody] CreatePromptFrameworkRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new CreatePromptFrameworkCommand(
            actorUserId,
            request.NameAr,
            request.NameEn,
            request.DescriptionAr,
            request.Category,
            request.Kind,
            request.PromptText,
            request.Variables,
            request.ExampleInput,
            request.ExampleOutput,
            request.Tags,
            request.RecommendedModel);
        Result<PromptFrameworkDto> result = await _sender.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, result.Value);
    }

    /// <summary>Updates a prompt framework's fields. Closes SEC-02 (was unauthenticated).</summary>
    /// <param name="promptId">The framework id to update.</param>
    /// <param name="request">The fields to change.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpPut("prompts/{promptId:int}")]
    [Authorize(Policy = "media_monitoring:edit")]
    public async Task<ActionResult<PromptFrameworkDto>> UpdatePromptAsync(
        int promptId, [FromBody] UpdatePromptFrameworkRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new UpdatePromptFrameworkCommand(
            actorUserId,
            promptId,
            request.NameAr,
            request.NameEn,
            request.DescriptionAr,
            request.PromptText,
            request.Variables,
            request.ExampleInput,
            request.ExampleOutput,
            request.Tags,
            request.IsApproved);
        Result<PromptFrameworkDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Deletes a prompt framework.</summary>
    /// <param name="promptId">The framework id to delete.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpDelete("prompts/{promptId:int}")]
    [Authorize(Policy = "media_monitoring:delete")]
    public async Task<ActionResult> DeletePromptAsync(int promptId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new DeletePromptFrameworkCommand(actorUserId, promptId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }

    /// <summary>Executes a prompt framework with variable substitution. Closes SEC-02 (was unauthenticated AI-cost).</summary>
    /// <param name="promptId">The framework id to run.</param>
    /// <param name="request">The variable substitution map.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the model output, or 404 if not found.</returns>
    [HttpPost("prompts/{promptId:int}/run")]
    [Authorize(Policy = "media_monitoring:view")]
    public async Task<ActionResult<RunPromptFrameworkResultDto>> RunPromptAsync(
        int promptId, [FromBody] RunPromptFrameworkRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        IReadOnlyDictionary<string, string> variables = request.Variables ?? new Dictionary<string, string>();
        Result<RunPromptFrameworkResultDto> result = await _sender.Send(new RunPromptFrameworkCommand(actorUserId, promptId, variables), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Runs one of the 7 fixed AI Quick text tools. Closes SEC-02 (was unauthenticated AI-cost).</summary>
    /// <param name="request">The tool selection and input.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the model output, or 400 for an unknown tool key.</returns>
    [HttpPost("ai/quick")]
    [Authorize(Policy = "media_monitoring:view")]
    public async Task<ActionResult<RunQuickAiToolResultDto>> RunQuickAiToolAsync([FromBody] RunQuickAiToolRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new RunQuickAiToolCommand(actorUserId, request.Tool, request.Input, request.Tone, request.Count);
        Result<RunQuickAiToolResultDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }
}
