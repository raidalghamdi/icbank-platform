namespace Icbank.Platform.Application.InternationalDays;

/// <summary>Ports a single row of <c>day_yearly_themes</c> (API-SURFACE.md §14).</summary>
/// <param name="Id">The theme row id.</param>
/// <param name="Year">The campaign year.</param>
/// <param name="ThemeAr">The optional Arabic theme text.</param>
/// <param name="ThemeEn">The optional English theme text.</param>
/// <param name="ThemeSourceUrl">The source URL for the theme, if any.</param>
public sealed record DayYearlyThemeDto(int Id, int Year, string? ThemeAr, string? ThemeEn, string? ThemeSourceUrl);
