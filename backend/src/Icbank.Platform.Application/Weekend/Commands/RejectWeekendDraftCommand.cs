using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>POST /weekend/drafts/:id/reject</c> (API-SURFACE.md §10). Admin-only. No status precondition — can reject from any state, including already-published (matches the Node source exactly).</summary>
/// <param name="ActorUserId">The rejecting admin's id.</param>
/// <param name="DraftId">The draft being rejected.</param>
/// <param name="Reason">The rejection reason, defaulted if not supplied.</param>
public sealed record RejectWeekendDraftCommand(int ActorUserId, int DraftId, string? Reason) : IRequest<Result<WeekendDraftDto>>;
