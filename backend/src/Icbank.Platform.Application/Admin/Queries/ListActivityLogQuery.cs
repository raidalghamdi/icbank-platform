using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Paginated, filterable activity-log query (API-SURFACE.md §5 <c>GET /admin/activity</c>).</summary>
/// <param name="Query">The paging parameters.</param>
/// <param name="UserId">Optional filter to a single acting user.</param>
/// <param name="Action">Optional filter to an exact action name.</param>
/// <param name="DateFrom">Optional inclusive lower bound on <c>CreatedAt</c> (UTC).</param>
/// <param name="DateTo">Optional inclusive upper bound on <c>CreatedAt</c> (UTC).</param>
public sealed record ListActivityLogQuery(PagedQuery Query, int? UserId, string? Action, DateTime? DateFrom, DateTime? DateTo)
    : IRequest<Result<PagedResult<ActivityLogEntryDto>>>;
