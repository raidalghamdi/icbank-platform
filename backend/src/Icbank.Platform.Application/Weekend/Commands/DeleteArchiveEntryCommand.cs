using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>DELETE /week-start/archive/:id</c> (API-SURFACE.md §8). Hard delete, matching the Node source.</summary>
/// <param name="ActorUserId">The deleting user's id.</param>
/// <param name="EntryId">The archive entry being deleted.</param>
public sealed record DeleteArchiveEntryCommand(int ActorUserId, int EntryId) : IRequest<Result<bool>>;
