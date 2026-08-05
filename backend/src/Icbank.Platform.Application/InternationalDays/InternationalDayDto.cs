namespace Icbank.Platform.Application.InternationalDays;

/// <summary>Ports a single row of <c>international_days</c> for API responses (API-SURFACE.md §14).</summary>
/// <param name="Id">The day id.</param>
/// <param name="DayNameAr">The Arabic day name.</param>
/// <param name="DayNameEn">The optional English day name.</param>
/// <param name="AnnualDate">The free-text annual date.</param>
/// <param name="Category">The optional category.</param>
/// <param name="OfficialOrganizer">The official organizing body, if known.</param>
/// <param name="OfficialOrganizerSource">The source URL for the organizer claim.</param>
/// <param name="HistorySummary">A summary of the day's history.</param>
/// <param name="HistorySource">The source URL for the history summary.</param>
/// <param name="Suggestions">AI-generated activation suggestions.</param>
/// <param name="LastSearchedAt">The UTC timestamp of the last AI search.</param>
public sealed record InternationalDayDto(
    int Id,
    string DayNameAr,
    string? DayNameEn,
    string? AnnualDate,
    string? Category,
    string? OfficialOrganizer,
    string? OfficialOrganizerSource,
    string? HistorySummary,
    string? HistorySource,
    IReadOnlyList<string> Suggestions,
    DateTimeOffset? LastSearchedAt);
