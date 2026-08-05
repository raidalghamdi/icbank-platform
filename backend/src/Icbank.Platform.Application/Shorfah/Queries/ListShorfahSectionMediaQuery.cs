using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>
/// Query for <c>GET /shorfah/sections/{id}/media</c> (API-SURFACE.md §19). Ports <c>shorfah.ts:548-556</c>,
/// ordered by display order, paginated (R-BE-033). Closes the AMBIGUOUS-API-4 gap for reads: the
/// initial Wave 4b port carried the Node source's global-view-only gate forward for this one
/// route while every mutation (upload/patch/delete) got the section-permission-tier check --
/// <paramref name="ActorUserId"/> lets the handler require the same view/contribute/review/approve
/// tier (or admin) as the mutations, closing the inconsistency.
/// </summary>
/// <param name="ActorUserId">The authenticated caller's id.</param>
/// <param name="SectionId">The section whose media is being read.</param>
/// <param name="Query">The pagination parameters.</param>
public sealed record ListShorfahSectionMediaQuery(int ActorUserId, int SectionId, PagedQuery Query) : IRequest<Result<PagedResult<ShorfahSectionMediaDto>>>;
