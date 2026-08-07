using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>POST /shorfah/sections/{id}/assign</c> (API-SURFACE.md §19). Ports <c>shorfah.ts:871-882</c>.</summary>
/// <param name="ActorUserId">The authenticated admin's id.</param>
/// <param name="SectionId">The section being assigned to.</param>
/// <param name="UserId">The user being assigned.</param>
/// <param name="Role">The assignment role label; defaults to <c>contributor</c> when omitted.</param>
public sealed record AssignShorfahSectionCommand(int ActorUserId, int SectionId, int UserId, string? Role) : IRequest<Result<ShorfahAssignmentDto>>;
