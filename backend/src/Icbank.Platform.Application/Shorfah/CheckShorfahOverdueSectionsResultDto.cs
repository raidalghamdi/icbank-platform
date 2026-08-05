namespace Icbank.Platform.Application.Shorfah;

/// <summary>The response shape for <c>POST /cron/shorfah/check-overdue</c> (BUSINESS-RULES.md §1.6).</summary>
/// <param name="OverdueSections">The number of sections found past their SLA deadline.</param>
/// <param name="Notified">The number of reminders actually sent this run (excludes same-day dedup skips).</param>
public sealed record CheckShorfahOverdueSectionsResultDto(int OverdueSections, int Notified);
