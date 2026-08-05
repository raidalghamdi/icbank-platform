using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.InternationalDays.Queries;

/// <summary>Ports <c>GET /intl-days/archive</c> (API-SURFACE.md §14). Closes DEFECT-LOG.md DATA-06's N+1 query pattern by batching theme/activation-count lookups.</summary>
/// <param name="Query">The pagination request.</param>
/// <param name="SearchText">Optional fuzzy match on dayNameAr/dayNameEn.</param>
/// <param name="Category">Optional category filter.</param>
/// <param name="Year">Optional year filter, scoping which yearly themes are returned per day.</param>
public sealed record ListInternationalDaysArchiveQuery(PagedQuery Query, string? SearchText, string? Category, int? Year)
    : IRequest<Result<PagedResult<InternationalDayArchiveItemDto>>>;
