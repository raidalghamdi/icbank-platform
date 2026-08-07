using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>PATCH /shorfah/sections/{id}/sla</c> (BUSINESS-RULES.md §1.6). Ports <c>shorfah.ts:854-869</c>.</summary>
/// <param name="ActorUserId">The authenticated admin's id.</param>
/// <param name="SectionId">The section whose SLA is being set.</param>
/// <param name="SlaDays">The new SLA day count, if changing.</param>
/// <param name="SlaStartsAt">The new SLA clock start, if changing; when set, the deadline is recomputed as <c>SlaStartsAt + SlaDays</c>.</param>
public sealed record UpdateShorfahSectionSlaCommand(int ActorUserId, int SectionId, int? SlaDays, DateTimeOffset? SlaStartsAt) : IRequest<Result<ShorfahSectionDto>>;
