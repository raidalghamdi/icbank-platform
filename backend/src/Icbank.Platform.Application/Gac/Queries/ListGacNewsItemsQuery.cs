using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Gac.Queries;

/// <summary>Ports <c>GET /gac/news</c> (API-SURFACE.md §12).</summary>
/// <param name="Query">The pagination request.</param>
/// <param name="Kind">Optional item-kind filter.</param>
public sealed record ListGacNewsItemsQuery(PagedQuery Query, string? Kind) : IRequest<Result<PagedResult<GacNewsItemDto>>>;
