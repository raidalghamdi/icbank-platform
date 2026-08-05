using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>POST /weekend/drafts/:id/approve</c> (API-SURFACE.md §10, BUSINESS-RULES.md §2.2). Admin-only. Hard precondition: status must be <c>pending_review</c>.</summary>
/// <param name="ActorUserId">The approving admin's id.</param>
/// <param name="DraftId">The draft being approved.</param>
public sealed record ApproveWeekendDraftCommand(int ActorUserId, int DraftId) : IRequest<Result<WeekendDraftDto>>;
