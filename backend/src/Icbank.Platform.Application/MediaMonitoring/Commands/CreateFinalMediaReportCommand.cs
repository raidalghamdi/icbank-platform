using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Manually saves and locks a final report (<c>POST /final-media-reports</c>). Computes the
/// official report number (<c>GAC-MEDIA-{n}/{year}</c>, BUSINESS-RULES.md §5.2) and a
/// <c>content_sha256</c> integrity fingerprint at creation time. Once created, the row is
/// permanently immutable (SEC-16/immutability enforced at the controller level via the always-403
/// PUT/DELETE endpoints).
/// </summary>
/// <param name="ActorUserId">The id of the authenticated caller saving the report.</param>
/// <param name="Title">The report title.</param>
/// <param name="ReportType">The report cadence/type key.</param>
/// <param name="PeriodLabel">The human-readable period label.</param>
/// <param name="DateFrom">The UTC start of the covered date range.</param>
/// <param name="DateTo">The UTC end of the covered date range.</param>
/// <param name="Draft">The 8-section content to persist.</param>
public sealed record CreateFinalMediaReportCommand(
    int ActorUserId,
    string Title,
    string? ReportType,
    string PeriodLabel,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    FinalReportDraftDto Draft) : IRequest<Result<FinalMediaReportDto>>;
