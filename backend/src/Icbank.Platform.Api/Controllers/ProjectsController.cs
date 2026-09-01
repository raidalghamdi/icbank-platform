using Asp.Versioning;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Projects;
using Icbank.Platform.Application.Projects.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Serves the tracked project portfolio behind the projects page. Gated by the same
/// <c>performance_reports:view</c> policy as the executive report the page also shows.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class ProjectsController : ControllerBase
{
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
}
