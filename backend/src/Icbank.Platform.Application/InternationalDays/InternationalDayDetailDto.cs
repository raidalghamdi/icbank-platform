namespace Icbank.Platform.Application.InternationalDays;

/// <summary>Ports <c>GET /intl-days/:id</c>'s response (API-SURFACE.md §14): a day with its full themes/activations/sources.</summary>
/// <param name="Day">The day's core fields.</param>
/// <param name="Themes">All recorded yearly themes, newest first.</param>
/// <param name="Activations">All recorded activations, newest first.</param>
/// <param name="Sources">All recorded source citations.</param>
public sealed record InternationalDayDetailDto(
    InternationalDayDto Day,
    IReadOnlyList<DayYearlyThemeDto> Themes,
    IReadOnlyList<DayActivationDto> Activations,
    IReadOnlyList<IntlDaySourceDto> Sources);
