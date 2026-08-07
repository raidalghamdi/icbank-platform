using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>POST /weekend/drafts/:id/publish</c> (API-SURFACE.md §10, BUSINESS-RULES.md §2.2). Admin-only. Can publish from <c>approved</c> or <c>pending_review</c> (skips the approve step).</summary>
/// <param name="ActorUserId">The publishing admin's id.</param>
/// <param name="DraftId">The draft being published.</param>
public sealed record PublishWeekendDraftCommand(int ActorUserId, int DraftId) : IRequest<Result<WeekendDraftDto>>;
