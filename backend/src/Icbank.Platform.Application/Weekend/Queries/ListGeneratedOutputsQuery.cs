using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Ports <c>GET /week-start/outputs</c> (API-SURFACE.md §8: Node returned the latest 30, always) with the mandated pagination envelope.</summary>
/// <param name="Query">The paging parameters.</param>
public sealed record ListGeneratedOutputsQuery(PagedQuery Query) : IRequest<Result<PagedResult<GeneratedOutputDto>>>;
