using System.IO.Compression;
using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.AiYear;
using Icbank.Platform.Application.AiYear.Commands;
using Icbank.Platform.Application.AiYear.Queries;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>routes/ai-year.ts</c> (API-SURFACE.md §13), gated by the seeded <c>ai_year:{verb}</c>
/// policy family. Closes DEFECT-LOG.md DATA-06 (N+1 query pattern, see
/// <see cref="ListAiYearActivationsQueryHandler"/>) and DATA-05 (untransactioned multi-table
/// writes, see the create/update handlers). The ZIP-export endpoint now streams a real archive
/// (<see cref="AiYearArchiveStreamWriter"/>) and the report endpoint now returns real
/// <c>.docx</c> bytes (<see cref="IAiYearReportDocxRenderer"/>), closing the two Wave 2 binary
/// deferrals recorded in WAVE2-PORT-NOTES.md.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/ai-year")]
public sealed class AiYearController : ControllerBase
{
    private readonly ISender _sender;
    private readonly AiYearArchiveStreamWriter _archiveStreamWriter;
    private readonly IAiYearReportDocxRenderer _reportDocxRenderer;

    /// <summary>Initializes a new instance of the <see cref="AiYearController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch AI Year commands/queries.</param>
    /// <param name="archiveStreamWriter">The ZIP-archive streaming writer for the media export endpoint.</param>
    /// <param name="reportDocxRenderer">The real .docx renderer for the report endpoint.</param>
    public AiYearController(ISender sender, AiYearArchiveStreamWriter archiveStreamWriter, IAiYearReportDocxRenderer reportDocxRenderer)
    {
        _sender = sender;
        _archiveStreamWriter = archiveStreamWriter;
        _reportDocxRenderer = reportDocxRenderer;
    }

    /// <summary>Lists activations with media/metrics, filterable by month/type/channel/search text.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="month">Optional month filter.</param>
    /// <param name="type">Optional type filter.</param>
    /// <param name="channel">Optional channel filter.</param>
    /// <param name="q">Optional fuzzy search text.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated activation list.</returns>
    [HttpGet("activations")]
    [Authorize(Policy = "ai_year:view")]
    public async Task<ActionResult<PagedResult<AiYearActivationDto>>> ListActivationsAsync(
        [FromQuery] int page,
        [FromQuery] int pageSize,
        [FromQuery] int? month,
        [FromQuery] string? type,
        [FromQuery] string? channel,
        [FromQuery] string? q,
        CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? PagedQuery.DefaultPageSize : pageSize };
        Result<PagedResult<AiYearActivationDto>> result =
            await _sender.Send(new ListAiYearActivationsQuery(pagedQuery, month, type, channel, q), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Creates an activation with optional media and metrics.</summary>
    /// <param name="request">The new activation's fields.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the new activation.</returns>
    [HttpPost("activations")]
    [Authorize(Policy = "ai_year:create")]
    public async Task<ActionResult<AiYearActivationDto>> CreateActivationAsync(
        [FromBody] CreateAiYearActivationRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new CreateAiYearActivationCommand(
            actorUserId,
            request.Title,
            request.Month,
            request.Year,
            request.ActivationDate,
            request.Type,
            request.Channels,
            request.Description,
            request.Tags,
            request.Status,
            request.Reach,
            request.Engagement,
            request.Notes,
            request.Media,
            request.Metrics);
        Result<AiYearActivationDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? StatusCode(StatusCodes.Status201Created, result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Fetches a single activation with its media.</summary>
    /// <param name="activationId">The activation id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpGet("activations/{activationId:int}")]
    [Authorize(Policy = "ai_year:view")]
    public async Task<ActionResult<AiYearActivationDto>> GetByIdAsync(int activationId, CancellationToken cancellationToken)
    {
        Result<AiYearActivationDto> result = await _sender.Send(new GetAiYearActivationByIdQuery(activationId), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Updates an activation's fields and optionally replaces its channels/media/metrics.</summary>
    /// <param name="activationId">The activation id.</param>
    /// <param name="request">The fields to change.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the updated activation, or 404 if not found.</returns>
    [HttpPut("activations/{activationId:int}")]
    [Authorize(Policy = "ai_year:edit")]
    public async Task<ActionResult<AiYearActivationDto>> UpdateActivationAsync(
        int activationId, [FromBody] UpdateAiYearActivationRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new UpdateAiYearActivationCommand(
            actorUserId,
            activationId,
            request.Title,
            request.Month,
            request.ActivationDate,
            request.Type,
            request.Description,
            request.Tags,
            request.Status,
            request.Reach,
            request.Engagement,
            request.Notes,
            request.Channels,
            request.Media,
            request.Metrics);
        Result<AiYearActivationDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Deletes an activation (cascades to media/metrics/channels via real FK constraints).</summary>
    /// <param name="activationId">The activation id to delete.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpDelete("activations/{activationId:int}")]
    [Authorize(Policy = "ai_year:delete")]
    public async Task<ActionResult> DeleteActivationAsync(int activationId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new DeleteAiYearActivationCommand(actorUserId, activationId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }

    /// <summary>Returns aggregate stats by month/type/channel.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the stats payload.</returns>
    [HttpGet("stats")]
    [Authorize(Policy = "ai_year:view")]
    public async Task<ActionResult<AiYearStatsDto>> GetStatsAsync(CancellationToken cancellationToken)
    {
        Result<AiYearStatsDto> result = await _sender.Send(new GetAiYearStatsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Issues a presigned upload URL for activation media.</summary>
    /// <param name="request">The upload request fields.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the presigned upload descriptor, or 400/404 on validation failure.</returns>
    [HttpPost("upload-url")]
    [Authorize(Policy = "ai_year:create")]
    public async Task<ActionResult<PresignedUpload>> GetUploadUrlAsync([FromBody] AiYearUploadUrlRequest request, CancellationToken cancellationToken)
    {
        var query = new GetAiYearUploadUrlQuery(request.Name, request.ContentType, request.ActivationId, request.Month, request.FileSize);
        Result<PresignedUpload> result = await _sender.Send(query, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status404NotFound);
    }

    /// <summary>
    /// Streams the activation's media as a real ZIP archive directly to the response body (ports
    /// the Node source's <c>archiver</c> route, <c>ai-year.ts:360-437</c>). Replaces the Wave 2
    /// deferral that returned only the manifest JSON.
    /// </summary>
    /// <param name="activationId">The activation id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 with a streamed <c>application/zip</c> body, or 404 if not found/no media.</returns>
    [HttpGet("activations/{activationId:int}/zip")]
    [Authorize(Policy = "ai_year:view")]
    public async Task<IActionResult> GetActivationZipAsync(int activationId, CancellationToken cancellationToken)
    {
        Result<AiYearActivationMediaArchiveDto> manifestResult =
            await _sender.Send(new GetAiYearActivationMediaArchivePathsQuery(activationId), cancellationToken);
        if (!manifestResult.IsSuccess)
        {
            return NotFound(new { error = manifestResult.Error });
        }

        AiYearActivationMediaArchiveDto manifest = manifestResult.Value!;
        var asciiFallbackName = $"activation-{activationId}.zip";
        var encodedTitle = Uri.EscapeDataString(manifest.ActivationTitle);

        Response.ContentType = "application/zip";
        Response.Headers.ContentDisposition = $"attachment; filename=\"{asciiFallbackName}\"; filename*=UTF-8''{encodedTitle}.zip";

        await _archiveStreamWriter.WriteAsync(manifest.Entries, Response.Body, cancellationToken);
        return new EmptyResult();
    }

    /// <summary>
    /// Returns the AI Year report as a real <c>.docx</c> binary, matching the Node original
    /// exactly (<c>ai-year.ts:440-569</c>). Closes the Wave 2 regression noted in
    /// WAVE2-PORT-NOTES.md item 16, where this endpoint had been returning the assembled
    /// <see cref="AiYearReportDataDto"/> as JSON instead of a document binary.
    /// </summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the <c>.docx</c> bytes.</returns>
    [HttpPost("report")]
    [Authorize(Policy = "ai_year:view")]
    public async Task<IActionResult> GetReportDataAsync(CancellationToken cancellationToken)
    {
        Result<AiYearReportDataDto> result = await _sender.Send(new GetAiYearReportDataQuery(), cancellationToken);
        var docxBytes = await _reportDocxRenderer.RenderAsync(result.Value!, cancellationToken);
        return File(
            docxBytes,
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "AI-Year-2026-Report.docx");
    }

    /// <summary>
    /// Returns the AI Year report's underlying assembled data as JSON, kept available as a
    /// separate read path now that <see cref="GetReportDataAsync"/> returns document bytes
    /// (documented decision, see RENDERING-NOTES.md: a distinct endpoint rather than a
    /// content-negotiated/query-flag switch on the existing route, since the two responses have
    /// fundamentally different shapes and callers should be able to discover the JSON path from
    /// the route itself).
    /// </summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the report data as JSON.</returns>
    [HttpGet("report/data")]
    [Authorize(Policy = "ai_year:view")]
    public async Task<ActionResult<AiYearReportDataDto>> GetReportDataJsonAsync(CancellationToken cancellationToken)
    {
        Result<AiYearReportDataDto> result = await _sender.Send(new GetAiYearReportDataQuery(), cancellationToken);
        return Ok(result.Value);
    }
}
