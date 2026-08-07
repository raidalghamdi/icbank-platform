using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.MediaMonitoring.Queries;

/// <summary>Lists final media reports, optionally filtered by type/year (<c>GET /final-media-reports</c>). Intentionally public per the Node source's file header comment.</summary>
/// <param name="Query">The pagination parameters.</param>
/// <param name="ReportType">Optional report-type filter.</param>
/// <param name="Year">Optional year filter, matched against <c>IssueDate</c>.</param>
public sealed record ListFinalMediaReportsQuery(PagedQuery Query, string? ReportType, int? Year)
    : IRequest<Result<PagedResult<FinalMediaReportDto>>>;
