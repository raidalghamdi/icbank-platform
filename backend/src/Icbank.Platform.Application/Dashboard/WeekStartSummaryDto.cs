namespace Icbank.Platform.Application.Dashboard;

/// <summary>Week-Start-specific dashboard counters.</summary>
/// <param name="ThisMonthCount">Entries created in the current Riyadh calendar month.</param>
/// <param name="TotalCount">Total archive entry count.</param>
/// <param name="LastTitle">The most recently created entry's title, if any.</param>
public sealed record WeekStartSummaryDto(int ThisMonthCount, int TotalCount, string? LastTitle);
