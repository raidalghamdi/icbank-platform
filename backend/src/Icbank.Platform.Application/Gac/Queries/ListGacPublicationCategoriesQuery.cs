using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Gac.Queries;

/// <summary>Ports <c>GET /gac/publications/categories</c> (API-SURFACE.md §12): category counts for filter chips.</summary>
public sealed record ListGacPublicationCategoriesQuery : IRequest<Result<IReadOnlyList<GacPublicationCategoryCountDto>>>;
