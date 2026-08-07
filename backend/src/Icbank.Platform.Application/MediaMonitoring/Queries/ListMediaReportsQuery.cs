using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Queries;

/// <summary>Lists published media-monitoring reports, optionally filtered by audience/type (<c>GET /media-reports</c>).</summary>
/// <param name="Query">The pagination parameters.</param>
/// <param name="Audience">Optional audience-tier filter.</param>
/// <param name="ReportType">Optional report-type filter.</param>
public sealed record ListMediaReportsQuery(PagedQuery Query, string? Audience, string? ReportType)
    : IRequest<Result<PagedResult<MediaReportDto>>>;
