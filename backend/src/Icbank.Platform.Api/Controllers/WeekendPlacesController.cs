using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Storage;
using Icbank.Platform.Application.Weekend;
using Icbank.Platform.Application.Weekend.Commands;
using Icbank.Platform.Application.Weekend.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>routes/weekend-places.ts</c> (API-SURFACE.md §9). All admin routes now use the
/// centralized <c>weekend:{verb}</c> policies instead of the Node source's blanket
/// <c>requireAdmin</c> — a role with fine-grained <c>weekend:view</c> but not <c>weekend:delete</c>
/// can browse but not remove places, closing part of BUSINESS-RULES.md §10.3's "binary, not
/// graduated" access gap for this feature area.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class WeekendPlacesController : ControllerBase
{
    private const int DefaultPageSize = 25;

    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="WeekendPlacesController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch weekend-places commands/queries.</param>
    public WeekendPlacesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Public-facing weekend page payload: curated places merged with the latest published draft's content.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the merged payload.</returns>
    [HttpGet("wk2-data")]
    [Authorize]
    public async Task<ActionResult<Wk2DataDto>> GetWk2DataAsync(CancellationToken cancellationToken)
    {
        Result<Wk2DataDto> result = await _sender.Send(new GetWk2DataQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Lists ALL places including inactive.</summary>
    /// <param name="page">The 1-based page number.</param>
    /// <param name="pageSize">The page size (capped at 100 per R-BE-033).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the paginated place list.</returns>
    [HttpGet("weekend-places")]
    [Authorize(Policy = "weekend:view")]
    public async Task<ActionResult<PagedResult<WeekendPlaceDto>>> ListAsync([FromQuery] int page, [FromQuery] int pageSize, CancellationToken cancellationToken)
    {
        var pagedQuery = new PagedQuery { Page = page == 0 ? 1 : page, PageSize = pageSize == 0 ? DefaultPageSize : pageSize };
        Result<PagedResult<WeekendPlaceDto>> result = await _sender.Send(new ListWeekendPlacesQuery(pagedQuery), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Issues a presigned upload URL for a place image.</summary>
    /// <param name="request">The file name and optional content type.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the presigned upload descriptor, or 400 if fileName is missing.</returns>
    [HttpPost("weekend-places/upload-url")]
    [Authorize(Policy = "weekend:create")]
    public async Task<ActionResult<PresignedUpload>> GetUploadUrlAsync([FromBody] WeekendPlaceUploadUrlRequest request, CancellationToken cancellationToken)
    {
        Result<PresignedUpload> result = await _sender.Send(new GetWeekendPlaceUploadUrlQuery(request.FileName, request.ContentType), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Creates a new curated place.</summary>
    /// <param name="request">The new place's fields.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the new place.</returns>
    [HttpPost("weekend-places")]
    [Authorize(Policy = "weekend:create")]
    public async Task<ActionResult<WeekendPlaceDto>> CreateAsync([FromBody] CreateWeekendPlaceRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new CreateWeekendPlaceCommand(actorUserId, request.Name, request.Description, request.ImageUrl, request.City, request.MapsQuery, request.SortOrder);
        Result<WeekendPlaceDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Updates a place's fields (partial update).</summary>
    /// <param name="placeId">The place being updated.</param>
    /// <param name="request">The fields to change.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the updated place, or 404 if not found.</returns>
    [HttpPatch("weekend-places/{placeId:int}")]
    [Authorize(Policy = "weekend:edit")]
    public async Task<ActionResult<WeekendPlaceDto>> UpdateAsync(int placeId, [FromBody] UpdateWeekendPlaceRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new UpdateWeekendPlaceCommand(
            actorUserId, placeId, request.Name, request.Description, request.ImageUrl, request.City, request.MapsQuery, request.IsActive, request.SortOrder);
        Result<WeekendPlaceDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }

    /// <summary>Deletes a place.</summary>
    /// <param name="placeId">The place being deleted.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK, or 404 if not found.</returns>
    [HttpDelete("weekend-places/{placeId:int}")]
    [Authorize(Policy = "weekend:delete")]
    public async Task<ActionResult> DeleteAsync(int placeId, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        Result<bool> result = await _sender.Send(new DeleteWeekendPlaceCommand(actorUserId, placeId), cancellationToken);
        return result.IsSuccess ? Ok(new { ok = true }) : NotFound(new { error = result.Error });
    }
}
