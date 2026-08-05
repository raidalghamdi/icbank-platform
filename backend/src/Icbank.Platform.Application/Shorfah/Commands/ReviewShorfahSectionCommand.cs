using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>POST /shorfah/sections/{id}/review</c> (BUSINESS-RULES.md §1.3). Ports <c>shorfah.ts:420-441</c>.</summary>
/// <param name="ActorUserId">The authenticated reviewer's id.</param>
/// <param name="SectionId">The section being reviewed.</param>
/// <param name="Decision">Either <c>pass</c> or <c>reject</c>; any other/no value is treated as pass-through to <c>in_review</c>.</param>
/// <param name="Notes">Optional free-text notes; becomes the rejection reason when rejecting.</param>
public sealed record ReviewShorfahSectionCommand(int ActorUserId, int SectionId, string? Decision, string? Notes) : IRequest<Result<ShorfahSectionDto>>;
