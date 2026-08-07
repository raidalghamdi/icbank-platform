using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Ports <c>POST /shorfah/issues/:id/start-review</c> (API-SURFACE.md §19, BUSINESS-RULES.md
/// §1.1). Admin-only. Transitions to <c>in_review</c>; blocked only if already <c>published</c>
/// -- no guard preventing <c>collecting -> in_review</c> before any section is submitted, matching
/// the Node source's documented gap verbatim (not silently fixed by this port).
/// </summary>
/// <param name="ActorUserId">The admin's id.</param>
/// <param name="IssueId">The issue being transitioned.</param>
public sealed record StartShorfahIssueReviewCommand(int ActorUserId, int IssueId) : IRequest<Result<ShorfahIssueDto>>;
