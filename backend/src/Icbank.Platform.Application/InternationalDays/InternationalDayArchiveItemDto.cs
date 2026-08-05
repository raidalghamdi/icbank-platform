namespace Icbank.Platform.Application.InternationalDays;

/// <summary>Ports one row of <c>GET /intl-days/archive</c>'s response (API-SURFACE.md §14): a day plus its recent themes and activation count.</summary>
/// <param name="Day">The day's core fields.</param>
/// <param name="Themes">Up to 3 most recent yearly themes.</param>
/// <param name="ActivationCount">The total number of recorded activations for this day.</param>
public sealed record InternationalDayArchiveItemDto(InternationalDayDto Day, IReadOnlyList<DayYearlyThemeDto> Themes, int ActivationCount);
