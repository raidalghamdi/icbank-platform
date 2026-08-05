using Asp.Versioning;
using Icbank.Platform.Api.Auth;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Designs.IconEvent.Commands;
using Icbank.Platform.Application.Designs.IconEvent.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>routes/icon-event-designs.ts</c> (API-SURFACE.md §18, BUSINESS-RULES.md §7.4/§7.5).
/// The Node source gated this entire router with only <c>requireAuth</c> (not
/// <c>requireAdmin</c>), unlike the sibling <c>routes/designs.ts</c> which is
/// file-wide-admin-gated -- AMBIGUOUS-API-5 flags this inconsistency. This port unifies both
/// under the seeded <c>design_studio</c> page-slug policy family, so a role must be explicitly
/// granted <c>design_studio:{verb}</c> rather than merely being logged in; see
/// WAVE3B-PORT-NOTES.md for the behaviour-change sign-off item this implies.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/designs/icon-event")]
public sealed class IconEventDesignsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="IconEventDesignsController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch icon-event commands/queries.</param>
    public IconEventDesignsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Lists the available icon catalogue.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the icon catalogue.</returns>
    [HttpGet("icons")]
    [Authorize(Policy = "design_studio:view")]
    public async Task<ActionResult<IconEventIconCatalogDto>> ListIconsAsync(CancellationToken cancellationToken)
    {
        Result<IconEventIconCatalogDto> result = await _sender.Send(new ListIconEventIconsQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>AI-analyzes raw event data and generates 3 design variants. Rate limited and audited (external-cost abuse vector).</summary>
    /// <param name="request">The generation parameters.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the generated variants, or 429 if the caller's generation quota is exhausted.</returns>
    [HttpPost("generate")]
    [Authorize(Policy = "design_studio:create")]
    public async Task<ActionResult<GenerateIconEventDesignResultDto>> GenerateAsync(
        [FromBody] GenerateIconEventDesignRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new GenerateIconEventDesignCommand(
            actorUserId,
            request.RawData,
            request.Headline,
            request.Subtitle,
            request.Department,
            request.Hashtag,
            request.Date,
            request.Time,
            request.Location,
            request.EventType,
            request.Size,
            request.MainIconOverride);
        Result<GenerateIconEventDesignResultDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status429TooManyRequests);
    }

    /// <summary>Generates deterministic, no-AI HTML for one or more size presets from explicit fields.</summary>
    /// <param name="request">The studio parameters.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the rendered HTML per size.</returns>
    [HttpPost("studio")]
    [Authorize(Policy = "design_studio:create")]
    public async Task<ActionResult<GenerateIconEventStudioResultDto>> StudioAsync(
        [FromBody] GenerateIconEventStudioRequest request, CancellationToken cancellationToken)
    {
        var command = new GenerateIconEventStudioCommand(
            request.Headline, request.Subtitle, request.Department, request.MainIcon, request.Sizes, request.Layout, request.LogoUrl);
        Result<GenerateIconEventStudioResultDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Renders client-supplied HTML to a PNG image. Rate limited and audited (external-cost abuse vector).</summary>
    /// <param name="request">The HTML and size/quality parameters.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the saved image's object path, or 429 if the caller's render quota is exhausted.</returns>
    [HttpPost("render")]
    [Authorize(Policy = "design_studio:create")]
    public async Task<ActionResult<RenderIconEventDesignResultDto>> RenderAsync(
        [FromBody] RenderIconEventDesignRequest request, CancellationToken cancellationToken)
    {
        var actorUserId = CurrentUserId.TryRead(User) ?? throw new InvalidOperationException("Authenticated request missing subject claim.");
        var command = new RenderIconEventDesignCommand(actorUserId, request.Html, request.Size, request.Quality);
        Result<RenderIconEventDesignResultDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : Problem(result.Error, statusCode: StatusCodes.Status429TooManyRequests);
    }
}
