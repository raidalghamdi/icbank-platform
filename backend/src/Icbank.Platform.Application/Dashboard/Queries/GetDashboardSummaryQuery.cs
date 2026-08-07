using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Dashboard.Queries;

/// <summary>Ports <c>GET /dashboard/summary</c> (API-SURFACE.md §6, BUSINESS-RULES.md §9).</summary>
public sealed record GetDashboardSummaryQuery : IRequest<Result<DashboardSummaryDto>>;
