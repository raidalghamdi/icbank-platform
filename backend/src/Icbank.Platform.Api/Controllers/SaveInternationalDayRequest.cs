using Icbank.Platform.Application.InternationalDays;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="InternationalDaysController.SaveAsync"/>.</summary>
/// <param name="Data">The search result to persist.</param>
/// <param name="Category">The optional category to tag the day with.</param>
public sealed record SaveInternationalDayRequest(DaySearchResultDto Data, string? Category);
