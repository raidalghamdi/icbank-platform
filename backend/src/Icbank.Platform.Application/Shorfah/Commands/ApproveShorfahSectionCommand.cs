using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>POST /shorfah/sections/{id}/approve</c> (BUSINESS-RULES.md §1.3). Ports <c>shorfah.ts:452-467</c>.</summary>
/// <param name="ActorUserId">The authenticated approver's id.</param>
/// <param name="SectionId">The section being approved.</param>
/// <param name="Notes">Optional free-text notes.</param>
public sealed record ApproveShorfahSectionCommand(int ActorUserId, int SectionId, string? Notes) : IRequest<Result<ShorfahSectionDto>>;
