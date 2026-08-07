using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Admin.Queries;

/// <summary>Lists every role with its user count (API-SURFACE.md §5 <c>GET /admin/roles</c>).</summary>
/// <param name="Query">The paging parameters (R-BE-033 — even a currently-small, 9-row catalogue gets a pagination envelope so growth to custom roles never introduces an unbounded list).</param>
public sealed record ListRolesQuery(PagedQuery Query) : IRequest<Result<PagedResult<RoleSummaryDto>>>;
