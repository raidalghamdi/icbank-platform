using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>POST /shorfah/sections/{id}/submit</c> (BUSINESS-RULES.md §1.3). Ports <c>shorfah.ts:400-418</c>.</summary>
/// <param name="ActorUserId">The authenticated contributor's id.</param>
/// <param name="SectionId">The section being submitted.</param>
public sealed record SubmitShorfahSectionCommand(int ActorUserId, int SectionId) : IRequest<Result<ShorfahSectionDto>>;
