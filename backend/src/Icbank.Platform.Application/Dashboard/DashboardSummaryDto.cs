namespace Icbank.Platform.Application.Dashboard;

/// <summary>Ports the <c>GET /dashboard/summary</c> response shape (API-SURFACE.md §6, BUSINESS-RULES.md §9).</summary>
/// <param name="Kpi">The top-line KPI tile values.</param>
/// <param name="WeekStart">Week-Start-specific counters.</param>
/// <param name="AiYear">AI Year-specific counters and recent activations.</param>
/// <param name="IntlDaysUpcoming">The nearest upcoming international observance days (top 3).</param>
public sealed record DashboardSummaryDto(
    DashboardKpiDto Kpi,
    WeekStartSummaryDto WeekStart,
    AiYearSummaryDto AiYear,
    IReadOnlyList<UpcomingInternationalDayDto> IntlDaysUpcoming);
