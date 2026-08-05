using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Query for <c>GET /shorfah/sections/{id}/log</c> (API-SURFACE.md §19). Ports <c>shorfah.ts:536-541</c>, newest first, paginated (R-BE-033).</summary>
/// <param name="SectionId">The section whose log is being read.</param>
/// <param name="Query">The pagination parameters.</param>
public sealed record ListShorfahWorkflowLogQuery(int SectionId, PagedQuery Query) : IRequest<Result<PagedResult<ShorfahWorkflowLogDto>>>;
