namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PATCH /api/v1/shorfah/sections/{sectionId}/sla</c>.</summary>
/// <param name="SlaDays">The new SLA day count, if changing.</param>
/// <param name="SlaStartsAt">The new SLA clock start, if changing.</param>
public sealed record UpdateShorfahSectionSlaRequest(int? SlaDays, DateTimeOffset? SlaStartsAt);
