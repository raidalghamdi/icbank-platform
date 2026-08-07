using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Generates an audience-tiered media-monitoring report from cached LinkedIn/news feed data
/// (<c>POST /media-reports/generate</c>, BUSINESS-RULES.md §5.1). Closes DEFECT-LOG.md SEC-02:
/// the Node source ran this endpoint with no authentication at all; this port requires
/// <c>media_monitoring:create</c>.
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller generating the report.</param>
/// <param name="Audience">The target audience tier key, defaults to <c>manager</c> if unrecognized.</param>
/// <param name="ReportType">The report cadence/type key, defaults to <c>weekly</c> if unrecognized.</param>
/// <param name="DateFrom">The optional explicit range start; when omitted, derived from <paramref name="ReportType"/> (7 days for weekly, 30 for monthly).</param>
/// <param name="DateTo">The optional explicit range end; when omitted, defaults to now.</param>
/// <param name="Sources">The source list to include, e.g. <c>linkedin</c>/<c>news</c>.</param>
/// <param name="CustomTitle">An optional caller-supplied title override.</param>
public sealed record GenerateMediaReportCommand(
    int ActorUserId,
    string? Audience,
    string? ReportType,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    IReadOnlyList<string>? Sources,
    string? CustomTitle) : IRequest<Result<MediaReportDto>>;
