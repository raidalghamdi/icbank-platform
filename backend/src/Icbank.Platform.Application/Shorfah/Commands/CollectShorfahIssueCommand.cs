using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Ports <c>POST /shorfah/issues/:id/collect</c> (API-SURFACE.md §19, BUSINESS-RULES.md §1.1).
/// Idempotent: seeds the 13 canonical sections only if none exist yet, then sets status to
/// <c>collecting</c> unless already <c>published</c> (in which case status is left unchanged).
/// AMBIGUOUS-API-3 in API-SURFACE.md notes the Node source gated this with plain
/// <c>requireAuth</c> (not <c>requireAdmin</c>) unlike every other issue-lifecycle mutation --
/// this port preserves that lighter gate deliberately (see the controller's policy attribute and
/// WAVE4A-PORT-NOTES.md §4 for the product sign-off item).
/// </summary>
/// <param name="ActorUserId">The calling user's id.</param>
/// <param name="IssueId">The issue being collected.</param>
public sealed record CollectShorfahIssueCommand(int ActorUserId, int IssueId) : IRequest<Result<CollectShorfahIssueResultDto>>;
