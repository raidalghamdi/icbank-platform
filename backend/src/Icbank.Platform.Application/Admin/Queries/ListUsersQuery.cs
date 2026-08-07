using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Paginated/searchable admin user list (API-SURFACE.md §5 <c>GET /admin/users</c>).</summary>
/// <param name="Query">The paging parameters.</param>
/// <param name="Search">Optional case-insensitive substring match against email/name.</param>
public sealed record ListUsersQuery(PagedQuery Query, string? Search) : IRequest<Result<PagedResult<UserSummaryDto>>>;
