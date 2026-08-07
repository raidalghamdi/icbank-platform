using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>DELETE /shorfah/assignments/{id}</c> (API-SURFACE.md §19). Ports <c>shorfah.ts:883-887</c>.</summary>
/// <param name="ActorUserId">The authenticated admin's id.</param>
/// <param name="AssignmentId">The assignment being removed.</param>
public sealed record RemoveShorfahAssignmentCommand(int ActorUserId, int AssignmentId) : IRequest<Result<bool>>;
