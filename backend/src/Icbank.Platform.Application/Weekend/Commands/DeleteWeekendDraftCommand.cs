using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>DELETE /weekend/drafts/:id</c> (API-SURFACE.md §10). Admin-only. Hard delete, matching the Node source.</summary>
/// <param name="ActorUserId">The deleting admin's id.</param>
/// <param name="DraftId">The draft being deleted.</param>
public sealed record DeleteWeekendDraftCommand(int ActorUserId, int DraftId) : IRequest<Result<bool>>;
