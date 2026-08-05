using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Emails a final report to recipients (<c>POST /final-media-reports/:id/send-email</c>). Closes
/// DEFECT-LOG.md SEC-02: the Node source ran this unauthenticated email-send endpoint with no
/// authentication at all; this port requires <c>media_monitoring:view</c>.
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller sending the report.</param>
/// <param name="ReportId">The report id to send.</param>
/// <param name="Recipients">The recipient email addresses.</param>
/// <param name="Subject">The optional email subject override.</param>
public sealed record SendFinalMediaReportEmailCommand(int ActorUserId, int ReportId, IReadOnlyList<string> Recipients, string? Subject)
    : IRequest<Result<SendFinalMediaReportEmailResultDto>>;
