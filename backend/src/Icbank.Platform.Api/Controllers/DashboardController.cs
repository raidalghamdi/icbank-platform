using Asp.Versioning;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Dashboard;
using Icbank.Platform.Application.Dashboard.Commands;
using Icbank.Platform.Application.Dashboard.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>routes/dashboard.ts</c> (API-SURFACE.md §6). The Node source gated both routes only
/// by the blanket <c>requireAuth</c> (no page-specific check at all, despite a
/// <c>requirePageAccess("dashboard")</c> prefix rule existing for <c>/daily-report</c>,<c>/report</c>
/// elsewhere in <c>routes/index.ts</c>). This port closes that gap by applying the
/// <c>dashboard:view</c>/<c>dashboard:create</c> policies to the two routes respectively — every
/// endpoint now goes through the same centralized RBAC model the rest of the platform uses
/// (SEC-02 closure: no endpoint here is reachable by a merely-authenticated user with zero
/// dashboard grants).
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/dashboard")]
public sealed class DashboardController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="DashboardController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch dashboard queries/commands.</param>
    public DashboardController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>KPI summary: AI Year activation count, Week-Start counts, and upcoming international days.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the dashboard summary.</returns>
    [HttpGet("summary")]
    [Authorize(Policy = "dashboard:view")]
    public async Task<ActionResult<DashboardSummaryDto>> GetSummaryAsync(CancellationToken cancellationToken)
    {
        Result<DashboardSummaryDto> result = await _sender.Send(new GetDashboardSummaryQuery(), cancellationToken);
        return Ok(result.Value);
    }

    /// <summary>Generates an AI executive summary of recent internal-communications activity. Takes no input.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the generated summary.</returns>
    [HttpPost("ai-summary")]
    [Authorize(Policy = "dashboard:create")]
    public async Task<ActionResult<Icbank.Platform.Application.Dashboard.Commands.ExecutiveSummaryDto>> GenerateAiSummaryAsync(CancellationToken cancellationToken)
    {
        Result<Icbank.Platform.Application.Dashboard.Commands.ExecutiveSummaryDto> result = await _sender.Send(new GenerateExecutiveSummaryCommand(), cancellationToken);
        return Ok(result.Value);
    }
}
