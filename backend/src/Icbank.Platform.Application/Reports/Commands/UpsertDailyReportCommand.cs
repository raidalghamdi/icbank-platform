using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Reports.Commands;

/// <summary>
/// Ports <c>POST /daily-report</c> and <c>POST /report</c> (API-SURFACE.md §7) as a single
/// command — the two Node endpoints are structurally near-identical upserts against the same
/// table differing only in whether n8n field-name normalization is applied first
/// (BUSINESS-RULES.md §6, API-SURFACE.md §24 duplicate-endpoint analysis). This port collapses
/// them into one handler with a normalization flag rather than two near-duplicate handlers.
/// </summary>
/// <param name="ReportDate">The ISO (<c>yyyy-MM-dd</c>) report date.</param>
/// <param name="ReportDataJson">The raw report payload as JSON text, already normalized by the caller if <paramref name="ApplyN8NNormalization"/> is set from the n8n-flavored endpoint.</param>
/// <param name="ApplyN8NNormalization">Whether to apply n8n field-name remapping (<c>/report</c> path) or store the payload as-is (<c>/daily-report</c> path).</param>
public sealed record UpsertDailyReportCommand(string ReportDate, string ReportDataJson, bool ApplyN8NNormalization)
    : IRequest<Result<DailyReportDto>>;
