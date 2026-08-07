namespace Icbank.Platform.Application.InternationalDays.Commands;

/// <summary>The search outcome.</summary>
/// <param name="Cached">Whether this result was served from the 7-day cache instead of a fresh AI search.</param>
/// <param name="RemainingSearches">The caller's remaining search quota in the current rate-limit window.</param>
/// <param name="Category">The category the result was tagged with, if any.</param>
/// <param name="Data">The (not-yet-persisted) search result data.</param>
public sealed record SearchInternationalDayResultDto(bool Cached, int RemainingSearches, string? Category, DaySearchResultDto Data);
