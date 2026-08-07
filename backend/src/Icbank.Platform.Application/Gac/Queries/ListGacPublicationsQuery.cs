using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Gac.Queries;

/// <summary>
/// Ports <c>GET /gac/publications</c> (API-SURFACE.md §12). The Node source left this route
/// unauthenticated by mount-order accident; this port requires <c>[Authorize]</c> (closes SEC-02
/// for this route family) while keeping the search/category/language filters and result shape.
/// </summary>
/// <param name="Query">The pagination request.</param>
/// <param name="SearchText">Optional fuzzy match on titleAr/titleEn/descriptionAr.</param>
/// <param name="Category">Optional category filter.</param>
/// <param name="Language">Optional language filter.</param>
public sealed record ListGacPublicationsQuery(PagedQuery Query, string? SearchText, string? Category, string? Language)
    : IRequest<Result<PagedResult<GacPublicationDto>>>;
