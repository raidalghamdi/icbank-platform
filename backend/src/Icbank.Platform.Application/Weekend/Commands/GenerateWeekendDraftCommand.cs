using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>Ports <c>POST /weekend/generate</c> (API-SURFACE.md §10, BUSINESS-RULES.md §2.2/§2.3). Admin-only.</summary>
/// <param name="ActorUserId">The generating admin's id.</param>
/// <param name="WeekendDate">The requested target weekend date, or <c>null</c> to default to the next Riyadh Thursday.</param>
public sealed record GenerateWeekendDraftCommand(int ActorUserId, string? WeekendDate) : IRequest<Result<WeekendDraftDto>>;
