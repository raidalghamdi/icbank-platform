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
/// Ports <c>routes/week-start.ts</c> (API-SURFACE.md §8, BUSINESS-RULES.md §2.5). All routes use
/// the <c>weekstart:{verb}</c> policy family (RBAC-mapped to the distinct <c>weekstart</c> page
/// slug — this port does NOT reuse the shared <c>weekend</c> slug the Node source's
/// <c>requirePageAccess("weekend")</c> mapping used for this router; see BUSINESS-RULES.md
/// AMBIGUOUS-BR-4, resolved here in favor of the distinct-slug option since <c>PageSlugs.WeekStart</c>
/// ("weekstart") already exists as its own seeded page).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/week-start")]
[Authorize(Policy = "weekstart:view")]
public sealed class WeekStartController : ControllerBase
{
    private const int DefaultPageSize = 25;

    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="WeekStartController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch week-start commands/queries.</param>
    public WeekStartController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Uploads documents to build the style archive.</summary>
    /// <param name="files">The uploaded files (multipart form data).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with per-file results, or 400 on validation failure.</returns>
    [HttpPost("upload")]
    [Authorize(Policy = "weekstart:create")]
    [RequestSizeLimit(100_000_000)]
    public async Task<ActionResult<UploadArchiveDocumentsResultDto>> UploadAsync(List<IFormFile> files, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var documents = new List<UploadedDocument>();
        foreach (IFormFile file in files ?? new List<IFormFile>())
        {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream, cancellationToken);
            documents.Add(new UploadedDocument(file.FileName, file.ContentType, memoryStream.ToArray()));
        }

        Result<UploadArchiveDocumentsResultDto> result = await _sender.Send(new UploadArchiveDocumentsCommand(actorUserId, documents), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Lists archive entries.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{count, entries}</c> paginated.</returns>
    [HttpGet("archive")]
    public async Task<ActionResult> ListArchiveAsync([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? DefaultPageSize : pageSize };
        Result<PagedResult<ArchiveEntryDto>> result = await _sender.Send(new ListArchiveEntriesQuery(pagedQuery), cancellationToken);
        return Ok(new { count = result.Value!.Items.Count, entries = result.Value.Items, page = result.Value.Page, pageSize = result.Value.PageSize, total = result.Value.Total });
    }

    /// <summary>Gets the learned style-profile singleton.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the profile, or <c>null</c> if none exists yet.</returns>
    [HttpGet("style-profile")]
    public async Task<ActionResult<StyleProfileDto?>> GetStyleProfileAsync(CancellationToken cancellationToken)
    {
        Result<StyleProfileDto?> result = await _sender.Send(new GetStyleProfileQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Generates week-start message drafts (one per model).</summary>
    /// <param name="request">The topic/occasion/audience/tone/length parameters.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the generated outputs, or 400 on validation failure.</returns>
    [HttpPost("generate")]
    [Authorize(Policy = "weekstart:create")]
    public async Task<ActionResult<IReadOnlyList<GeneratedOutputDto>>> GenerateAsync([FromBody] GenerateWeekStartMessagesRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new GenerateWeekStartMessagesCommand(actorUserId, request.Topic, request.Occasion, request.Audience, request.Tone, request.Length);
        Result<IReadOnlyList<GeneratedOutputDto>> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Marks a generated output as selected and archives it.</summary>
    /// <param name="request">The generated output id.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the selected output, or 400 on failure.</returns>
    [HttpPost("approve")]
    [Authorize(Policy = "weekstart:edit")]
    public async Task<ActionResult<GeneratedOutputDto>> ApproveAsync([FromBody] ApproveGeneratedOutputRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<GeneratedOutputDto> result = await _sender.Send(new ApproveGeneratedOutputCommand(actorUserId, request.Id), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Deletes an archive entry.</summary>
    /// <param name="entryId">The archive entry being deleted.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 400 on failure.</returns>
    [HttpDelete("archive/{entryId:int}")]
    [Authorize(Policy = "weekstart:delete")]
    public async Task<ActionResult> DeleteArchiveEntryAsync(int entryId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new DeleteArchiveEntryCommand(actorUserId, entryId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Lists generated week-start outputs.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated output list.</returns>
    [HttpGet("outputs")]
    public async Task<ActionResult<PagedResult<GeneratedOutputDto>>> ListOutputsAsync([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? DefaultPageSize : pageSize };
        Result<PagedResult<GeneratedOutputDto>> result = await _sender.Send(new ListGeneratedOutputsQuery(pagedQuery), cancellationToken);
        return Ok(result.Value);
    }
}
