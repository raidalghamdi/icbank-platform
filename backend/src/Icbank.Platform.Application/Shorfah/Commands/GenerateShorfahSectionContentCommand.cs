using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>POST /shorfah/sections/{id}/generate</c> (BUSINESS-RULES.md §1.8). Ports <c>shorfah.ts:471-513</c>.</summary>
/// <param name="ActorUserId">The authenticated admin's id.</param>
/// <param name="SectionId">The section being generated.</param>
public sealed record GenerateShorfahSectionContentCommand(int ActorUserId, int SectionId) : IRequest<Result<ShorfahSectionDto>>;
