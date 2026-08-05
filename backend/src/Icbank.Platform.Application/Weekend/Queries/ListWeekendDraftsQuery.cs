using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Queries;

/// <summary>Ports <c>GET /weekend/drafts</c> (API-SURFACE.md §10). Admin-only.</summary>
/// <param name="Query">The paging parameters (task requirement: no unbounded lists).</param>
/// <param name="Status">Optional exact-match status filter.</param>
public sealed record ListWeekendDraftsQuery(PagedQuery Query, string? Status) : IRequest<Result<PagedResult<WeekendDraftDto>>>;
