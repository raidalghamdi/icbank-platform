using Icbank.Platform.Application.Common.Models;
using MediatR;

namespace Icbank.Platform.Application.Shorfah.Commands;

/// <summary>Command for <c>PUT /shorfah/sla-defaults</c> (BUSINESS-RULES.md §1.5). Ports <c>shorfah.ts:276-316</c>.</summary>
/// <param name="ActorUserId">The authenticated admin's id.</param>
/// <param name="Defaults">The new per-section-type SLA-day defaults.</param>
/// <param name="Propagate">Whether to retroactively apply the new default to pending/rejected sections of the matching type; defaults to <c>true</c> when omitted.</param>
public sealed record UpdateShorfahSlaDefaultsCommand(int ActorUserId, IReadOnlyList<ShorfahSlaDefaultEntry> Defaults, bool? Propagate)
    : IRequest<Result<UpdateShorfahSlaDefaultsResultDto>>;
