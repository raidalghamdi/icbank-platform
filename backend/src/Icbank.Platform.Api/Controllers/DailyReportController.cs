using Asp.Versioning;
using Icbank.Platform.Api.Extensions;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Reports;
using Icbank.Platform.Application.Reports.Commands;
using Icbank.Platform.Application.Reports.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Icbank.Platform.Api.Controllers;

/// <summary>
/// Ports <c>routes/daily-report.ts</c> (API-SURFACE.md §7). The two upsert endpoints share one
/// handler with a normalization flag (see <see cref="UpsertDailyReportCommand"/>); the two
/// "latest" GET endpoints share one handler and are now both gated behind the same
/// <c>dashboard:view</c> policy (they were effectively public in the Node source due to router
/// mount ordering — AMBIGUOUS-API-1, closed here as a deliberate SEC-02 fix; see
/// WAVE1-PORT-NOTES.md). The two POST endpoints reuse the existing shared-secret
/// <c>cron-api-key</c> policy rather than introducing a second parallel API-key mechanism —
/// deliberately consolidated, see WAVE1-PORT-NOTES.md.
/// </summary>
[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}")]
public sealed class DailyReportController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>Initializes a new instance of the <see cref="DailyReportController"/> class.</summary>
    /// <param name="sender">The MediatR sender used to dispatch daily-report commands/queries.</param>
    public DailyReportController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>Upserts a daily report by date, using the strict internal schema.</summary>
    /// <param name="request">The raw JSON body: <c>{reportDate, reportData}</c>.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the upserted report, or 400 on validation failure.</returns>
    [HttpPost("daily-report")]
    [Authorize(Policy = AuthorizationPolicyExtensions.CronApiKeyPolicyName)]
    public async Task<ActionResult<DailyReportDto>> UpsertStrictAsync(
        [FromBody] DailyReportUpsertRequest request, CancellationToken cancellationToken)
    {
        var command = new UpsertDailyReportCommand(request.ReportDate, request.ReportData.GetRawText(), ApplyN8NNormalization: false);
        Result<DailyReportDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Upserts a daily report using the flexible, n8n-flavored field-normalizing schema.</summary>
    /// <param name="rawPayload">The raw n8n JSON body (freeform, passthrough).</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>201 Created with the upserted report, or 400 if <c>report_date</c>/<c>reportDate</c> is missing/invalid.</returns>
    [HttpPost("report")]
    [Authorize(Policy = AuthorizationPolicyExtensions.CronApiKeyPolicyName)]
    public async Task<ActionResult<DailyReportDto>> UpsertN8NAsync(
        [FromBody] System.Text.Json.JsonElement rawPayload, CancellationToken cancellationToken)
    {
        var rawJson = rawPayload.GetRawText();
        var reportDate = N8NPayloadNormalizer.ExtractReportDate(rawJson);
        if (string.IsNullOrWhiteSpace(reportDate))
        {
            return Problem("report_date is required (YYYY-MM-DD)", statusCode: StatusCodes.Status400BadRequest);
        }

        var command = new UpsertDailyReportCommand(reportDate, rawJson, ApplyN8NNormalization: true);
        Result<DailyReportDto> result = await _sender.Send(command, cancellationToken);
        return result.IsSuccess
            ? StatusCode(StatusCodes.Status201Created, result.Value)
            : Problem(result.Error, statusCode: StatusCodes.Status400BadRequest);
    }

    /// <summary>Fetches the most recent daily report by date.</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the latest report, or 404 if none exists.</returns>
    [HttpGet("daily-report/latest")]
    [Authorize(Policy = "dashboard:view")]
    public Task<ActionResult<DailyReportDto>> GetLatestAsync(CancellationToken cancellationToken) => GetLatestInternalAsync(cancellationToken);

    /// <summary>Alias of <see cref="GetLatestAsync"/> (API-SURFACE.md §24: byte-for-byte duplicate handler in the Node source).</summary>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>200 OK with the latest report, or 404 if none exists.</returns>
    [HttpGet("report/latest")]
    [Authorize(Policy = "dashboard:view")]
    public Task<ActionResult<DailyReportDto>> GetLatestAliasAsync(CancellationToken cancellationToken) => GetLatestInternalAsync(cancellationToken);

    private async Task<ActionResult<DailyReportDto>> GetLatestInternalAsync(CancellationToken cancellationToken)
    {
        Result<DailyReportDto> result = await _sender.Send(new GetLatestDailyReportQuery(), cancellationToken);
        return result.IsSuccess ? Ok(result.Value) : NotFound(new { error = result.Error });
    }
}
