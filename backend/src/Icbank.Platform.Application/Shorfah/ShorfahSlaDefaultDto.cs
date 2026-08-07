namespace Icbank.Platform.Application.Shorfah;

/// <summary>The Shorfah SLA-default response shape (BUSINESS-RULES.md §1.5).</summary>
/// <param name="SectionType">The section type this default applies to.</param>
/// <param name="SlaDays">The default SLA day count.</param>
public sealed record ShorfahSlaDefaultDto(string SectionType, int SlaDays);
