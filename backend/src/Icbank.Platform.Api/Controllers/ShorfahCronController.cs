using Asp.Versioning;
using Icbank.Platform.Api.Extensions;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Application.Shorfah.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Service-to-service cron trigger for Shorfah SLA/overdue processing (API-SURFACE.md §11
/// <c>routes/shorfah-cron.ts</c>, ported here as <c>POST /api/v1/shorfah/cron/check-overdue</c> --
/// see WAVE4B-PORT-NOTES.md for the Node-path-to-.NET-path mapping rationale). Closes SEC-13:
/// authentication is the configuration-bound <c>cron-api-key</c> policy -- if <c>Cron:ApiKey</c>
/// is unset, every request is rejected; there is no hardcoded fallback secret anywhere in this
/// codebase.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shorfah/cron")]
public sealed class ShorfahCronController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="ShorfahCronController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch the overdue-check command.</param>
    public ShorfahCronController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Triggers the SLA/overdue sweep. The actual sweep logic is ported alongside the Shorfah feature area; this endpoint's job is proving the auth gate.</summary>
    /// <returns>202 Accepted once authenticated by the cron API key.</returns>
    [HttpPost("overdue-sweep")]
    [Authorize(Policy = AuthorizationPolicyExtensions.CronApiKeyPolicyName)]
    public ActionResult TriggerOverdueSweep() => Accepted();

    /// <summary>
    /// Scans overdue sections and sends reminder notifications (BUSINESS-RULES.md §1.6). Ports
    /// <c>POST /cron/shorfah/check-overdue</c>. Idempotent: at most one overdue reminder is sent
    /// per section/recipient per Riyadh calendar day, so calling this twice in a row (or once an
    /// hour, indefinitely) never double-sends.
    /// </summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with <c>{ok, overdueSections, notified}</c>.</returns>
    [HttpPost("check-overdue")]
    [Authorize(Policy = AuthorizationPolicyExtensions.CronApiKeyPolicyName)]
    public async Task<ActionResult> CheckOverdueAsync(CancellationToken cancellationToken)
    {
        Result<CheckShorfahOverdueSectionsResultDto> result = await _sender.Send(new CheckShorfahOverdueSectionsCommand(), cancellationToken);
        return Ok(new { ok = true, overdueSections = result.Value!.OverdueSections, notified = result.Value.Notified });
    }
}
