using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Reports.Queries;

/// <summary>
/// Ports <c>GET /daily-report/latest</c> and <c>GET /report/latest</c> (API-SURFACE.md §7 —
/// byte-for-byte identical handler logic in the Node source; collapsed to one query here). The
/// Node source had no auth on either route (AMBIGUOUS-API-1: dead-code <c>requirePageAccess</c>
/// due to router mount ordering). This port closes that gap by requiring the
/// <c>dashboard:view</c> policy — see WAVE1-PORT-NOTES.md.
/// </summary>
public sealed record GetLatestDailyReportQuery : IRequest<Result<DailyReportDto>>;
