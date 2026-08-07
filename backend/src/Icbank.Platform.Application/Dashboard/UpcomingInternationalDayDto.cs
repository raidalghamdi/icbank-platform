namespace Icbank.Platform.Application.Dashboard;

/// <summary>An international day falling within the dashboard's 30-day lookahead window.</summary>
/// <param name="Id">The day's id.</param>
/// <param name="Name">The Arabic day name.</param>
/// <param name="Date">The ISO date of the next occurrence.</param>
/// <param name="DaysUntil">Days remaining until the next occurrence.</param>
/// <param name="Category">The day's category, if any.</param>
public sealed record UpcomingInternationalDayDto(int Id, string Name, string Date, int DaysUntil, string? Category);
