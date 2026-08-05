using Asp.Versioning;
using Icbank.Platform.Api.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Service-to-service cron trigger for Shorfah SLA/overdue processing (API-SURFACE.md §11
/// <c>routes/shorfah-cron.ts</c>). Closes SEC-13: authentication is the configuration-bound
/// <c>cron-api-key</c> policy — if <c>Cron:ApiKey</c> is unset, every request is rejected; there
/// is no hardcoded fallback secret anywhere in this codebase.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/shorfah/cron")]
public sealed class ShorfahCronController : ControllerBase
{
    /// <summary>Triggers the SLA/overdue sweep. The actual sweep logic is ported alongside the Shorfah feature area; this endpoint's job is proving the auth gate.</summary>
    /// <returns>202 Accepted once authenticated by the cron API key.</returns>
    [HttpPost("overdue-sweep")]
    [Authorize(Policy = AuthorizationPolicyExtensions.CronApiKeyPolicyName)]
    public ActionResult TriggerOverdueSweep() => Accepted();
}
