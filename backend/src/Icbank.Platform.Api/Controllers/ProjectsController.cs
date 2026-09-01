using System.Security.Claims;
using Asp.Versioning;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Projects;
using Icbank.Platform.Application.Projects.Commands;
using Icbank.Platform.Application.Projects.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Serves the tracked project portfolio behind the projects page. Reads are gated by the same
/// <c>performance_reports:view</c> policy as the executive report the page also shows; logging
/// progress is a write, so it requires the page's <c>performance_reports:edit</c> grant.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class ProjectsController : ControllerBase
{
    private const string UnknownReporter = "غير محدد";

    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="ProjectsController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch portfolio queries.</param>
    public ProjectsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Returns every tracked project with its schedule-derived tracking signal and the portfolio headline figures.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the portfolio payload.</returns>
    [HttpGet("projects/portfolio")]
    [Authorize(Policy = "performance_reports:view")]
    public async Task<ActionResult<ProjectPortfolioDto>> GetPortfolioAsync(CancellationToken cancellationToken)
    {
        Result<ProjectPortfolioDto> result = await _sender.Send(new GetProjectPortfolioQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Records one progress report against a tracked project and returns the recomputed card.</summary>
    /// <param name="projectId">The project to report against.</param>
    /// <param name="request">The reported percentage, the note, and optionally the reporter's name.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the recomputed project card, 400 on invalid input, or 404 when the project is missing or no longer tracked.</returns>
    [HttpPost("projects/{projectId:int}/progress")]
    [Authorize(Policy = "performance_reports:edit")]
    public async Task<ActionResult<PortfolioProjectDto>> RecordProgressAsync(
        int projectId,
        [FromBody] RecordProjectProgressRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        // Why: the reporter's identity comes from the token by default so a caller cannot log
        // progress under someone else's name; the body value is only a fallback for tokens issued
        // without a display name.
        var reportedBy = User.FindFirstValue(ClaimTypes.Name);
        if (string.IsNullOrWhiteSpace(reportedBy))
        {
            reportedBy = string.IsNullOrWhiteSpace(request.ReportedBy) ? UnknownReporter : request.ReportedBy;
        }

        var command = new RecordProjectProgressCommand(projectId, request.ProgressPercent, request.Note ?? string.Empty, reportedBy);
        Result<PortfolioProjectDto> result = await _sender.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            return Ok(result.Value);
        }

        var statusCode = result.Error == RecordProjectProgressCommand.ProjectNotFoundError
            ? StatusCodes.Status404NotFound
            : StatusCodes.Status400BadRequest;
        return Problem(result.Error, statusCode: statusCode);
    }
}
