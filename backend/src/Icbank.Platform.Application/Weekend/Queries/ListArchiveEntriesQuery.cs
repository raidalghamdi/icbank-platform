using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>
/// Ports <c>GET /week-start/archive</c> (API-SURFACE.md §8: Node returned the latest 50, always).
/// This port applies the mandated pagination envelope instead of a hardcoded 50-row cap (task
/// requirement: "no unbounded lists, even where the Node version returned everything") — see
/// WAVE1-PORT-NOTES.md.
/// </summary>
/// <param name="Query">The paging parameters.</param>
public sealed record ListArchiveEntriesQuery(PagedQuery Query) : IRequest<Result<PagedResult<ArchiveEntryDto>>>;
