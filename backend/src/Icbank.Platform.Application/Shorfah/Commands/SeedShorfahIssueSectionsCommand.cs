using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>
/// Ports <c>POST /shorfah/issues/:id/seed-sections</c> (API-SURFACE.md §19, BUSINESS-RULES.md
/// §1.2). Admin-only. Refuses if the issue already has any sections.
/// </summary>
/// <param name="ActorUserId">The admin's id.</param>
/// <param name="IssueId">The issue to backfill sections for.</param>
public sealed record SeedShorfahIssueSectionsCommand(int ActorUserId, int IssueId) : IRequest<Result<int>>;
