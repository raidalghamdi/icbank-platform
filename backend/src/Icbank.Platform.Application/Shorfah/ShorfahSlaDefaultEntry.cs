namespace Icbank.Platform.Application.Shorfah;

/// <summary>One row of the <c>PUT /shorfah/sla-defaults</c> request payload (BUSINESS-RULES.md §1.5).</summary>
/// <param name="SectionType">The section type this default applies to.</param>
/// <param name="SlaDays">The requested SLA day count, clamped server-side to [1, 60].</param>
public sealed record ShorfahSlaDefaultEntry(string SectionType, int SlaDays);
