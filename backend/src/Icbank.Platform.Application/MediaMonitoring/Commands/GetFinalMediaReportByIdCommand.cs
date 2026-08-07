using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Fetches a single final media report and increments its view counter
/// (<c>GET /final-media-reports/:id</c>). Modeled as a command, not a query, because it has a
/// side effect (the view-count write) -- the Node source fired this increment without awaiting
/// it (race-prone); this port awaits it inline for correctness (closes the fire-and-forget
/// pattern this endpoint used, matching R-BE-060's "no unawaited side effects" intent).
/// </summary>
/// <param name="ReportId">The report id.</param>
public sealed record GetFinalMediaReportByIdCommand(int ReportId) : IRequest<Result<FinalMediaReportDetailDto>>;
