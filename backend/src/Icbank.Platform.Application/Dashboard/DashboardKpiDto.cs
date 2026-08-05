namespace Icbank.Platform.Application.Dashboard;

/// <summary>The top-line KPI tile values shown on the dashboard.</summary>
/// <param name="AiYearActivations">Total AI Year activation count.</param>
/// <param name="WeekStartThisMonth">Week-Start archive entries created in the current Riyadh calendar month.</param>
/// <param name="WeekStartTotal">Total Week-Start archive entry count.</param>
/// <param name="IntlDaysUpcomingCount">Count of international days falling within the next 30 days.</param>
public sealed record DashboardKpiDto(int AiYearActivations, int WeekStartThisMonth, int WeekStartTotal, int IntlDaysUpcomingCount);
