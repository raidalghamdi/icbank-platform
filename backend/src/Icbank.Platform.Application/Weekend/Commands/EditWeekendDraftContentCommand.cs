using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>PATCH /weekend/drafts/:id</c> (API-SURFACE.md §10). Admin-only manual content edit before approval.</summary>
/// <param name="ActorUserId">The editing admin's id.</param>
/// <param name="DraftId">The draft being edited.</param>
/// <param name="ContentJson">The replacement content payload as JSON text.</param>
public sealed record EditWeekendDraftContentCommand(int ActorUserId, int DraftId, string ContentJson) : IRequest<Result<WeekendDraftDto>>;
