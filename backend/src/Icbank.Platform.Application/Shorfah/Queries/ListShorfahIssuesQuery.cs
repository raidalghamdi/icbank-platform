using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>
/// Ports <c>GET /shorfah/issues</c> (API-SURFACE.md §19). Ordered by year/month descending,
/// matching <c>shorfah.ts:84-90</c>'s <c>orderBy(desc(year), desc(month))</c>.
/// </summary>
/// <param name="Query">The paging parameters (task requirement: no unbounded lists; the Node source returned every issue unpaginated).</param>
public sealed record ListShorfahIssuesQuery(PagedQuery Query) : IRequest<Result<PagedResult<ShorfahIssueDto>>>;
