using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Ports <c>GET /shorfah/issues/:id</c> (API-SURFACE.md §19). Returns the issue and its sections, ordered by display order.</summary>
/// <param name="IssueId">The issue being fetched.</param>
public sealed record GetShorfahIssueByIdQuery(int IssueId) : IRequest<Result<ShorfahIssueDetailDto>>;
