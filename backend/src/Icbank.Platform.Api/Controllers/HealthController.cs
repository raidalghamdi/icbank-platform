using Asp.Versioning;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports the Node <c>/healthz</c> liveness endpoint (API-SURFACE.md §1). The Node source also
/// exposed <c>GET /debug/db</c> and <c>GET /debug/env</c> with no authentication whatsoever,
/// leaking <c>system_settings</c> contents (including Azure AD secrets) and DB host/port/NODE_ENV
/// respectively — defect DATA-03/C-3, a public information-leak defect. Both are deliberately
/// NOT ported here; see WAVE1-PORT-NOTES.md. The richer readiness surface (SQL Server + cache
/// checks) already exists at <c>/health/live</c>/<c>/health/ready</c>
/// (<see cref="Extensions.HealthCheckExtensions"/>) — this controller exists only to preserve the
/// exact legacy response shape/path for any caller still pointed at <c>/api/v1/healthz</c>.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class HealthController : ControllerBase
{
    /// <summary>Liveness probe. Never requires authentication (task instruction: "Only health/liveness may be anonymous").</summary>
    /// <returns>200 OK with <c>{status:"ok"}</c>, matching the Node response shape verbatim.</returns>
    [HttpGet("healthz")]
    [Microsoft.AspNetCore.Authorization.AllowAnonymous]
    public ActionResult<HealthCheckResponse> GetHealthz() => Ok(new HealthCheckResponse("ok"));
}
