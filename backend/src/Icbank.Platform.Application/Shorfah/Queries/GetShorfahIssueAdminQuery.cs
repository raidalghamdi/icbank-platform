using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Queries;

/// <summary>Ports <c>GET /shorfah/issues/:id/admin</c> (API-SURFACE.md §19). Requires the elevated policy in addition to resource existence.</summary>
/// <param name="IssueId">The issue being fetched.</param>
public sealed record GetShorfahIssueAdminQuery(int IssueId) : IRequest<Result<ShorfahIssueAdminDto>>;
