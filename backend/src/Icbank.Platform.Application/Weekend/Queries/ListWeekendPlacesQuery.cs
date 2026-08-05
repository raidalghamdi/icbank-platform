using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Ports <c>GET /weekend-places</c> (API-SURFACE.md §9): lists ALL places including inactive. Admin-only.</summary>
/// <param name="Query">The paging parameters (task requirement: no unbounded lists even where Node returned everything).</param>
public sealed record ListWeekendPlacesQuery(PagedQuery Query) : IRequest<Result<PagedResult<WeekendPlaceDto>>>;
