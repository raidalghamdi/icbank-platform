using Icbank.Platform.Application.Shorfah;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PUT /api/v1/shorfah/sla-defaults</c>.</summary>
/// <param name="Defaults">The new per-section-type SLA-day defaults.</param>
/// <param name="Propagate">Whether to retroactively apply the new default to pending/rejected sections; defaults to <c>true</c> when omitted.</param>
public sealed record UpdateShorfahSlaDefaultsRequest(IReadOnlyList<ShorfahSlaDefaultEntry> Defaults, bool? Propagate);
