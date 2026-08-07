using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Queries;

/// <summary>Fetches a single media-monitoring report by id (<c>GET /media-reports/:id</c>).</summary>
/// <param name="ReportId">The report id.</param>
public sealed record GetMediaReportByIdQuery(int ReportId) : IRequest<Result<MediaReportDto>>;
