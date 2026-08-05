using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Designs.Composer.Queries;

/// <summary>Ports <c>GET /designs/fonts</c> (API-SURFACE.md §17).</summary>
/// <param name="Paging">The pagination parameters.</param>
public sealed record ListBrandFontsQuery(PagedQuery Paging) : IRequest<Result<PagedResult<BrandFontDto>>>;
