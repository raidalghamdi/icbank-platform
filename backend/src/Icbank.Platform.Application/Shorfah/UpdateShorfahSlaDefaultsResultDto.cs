namespace Icbank.Platform.Application.Shorfah;

/// <summary>The response shape for <c>PUT /shorfah/sla-defaults</c> (BUSINESS-RULES.md §1.5).</summary>
/// <param name="Defaults">The full, current set of SLA defaults after the update.</param>
/// <param name="PropagatedSections">The number of existing sections whose <c>SlaDays</c> was retroactively updated.</param>
public sealed record UpdateShorfahSlaDefaultsResultDto(IReadOnlyList<ShorfahSlaDefaultDto> Defaults, int PropagatedSections);
