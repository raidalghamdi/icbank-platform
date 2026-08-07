namespace Icbank.Platform.Application.InternationalDays;

/// <summary>Input model for <see cref="InternationalDayHtmlExportBuilder"/>.</summary>
/// <param name="DayNameAr">The Arabic day name.</param>
/// <param name="DayNameEn">The optional English day name.</param>
/// <param name="AnnualDate">The free-text annual date.</param>
/// <param name="OfficialOrganizer">The official organizing body, if known.</param>
/// <param name="Category">The optional category.</param>
/// <param name="HistorySummary">A summary of the day's history.</param>
/// <param name="HistorySource">The source URL for the history summary.</param>
/// <param name="CurrentYearLabel">The current year, as displayed text.</param>
/// <param name="ThemeAr">The latest recorded Arabic theme.</param>
/// <param name="ThemeEn">The latest recorded English theme.</param>
/// <param name="ThemeSourceUrl">The source URL for the theme.</param>
/// <param name="Activations">The recorded activations.</param>
/// <param name="Suggestions">The AI-generated activation suggestions.</param>
/// <param name="Sources">The recorded source citations.</param>
/// <param name="ExportedAtLabel">The formatted export timestamp.</param>
public sealed record InternationalDayExportModel(
    string DayNameAr,
    string? DayNameEn,
    string? AnnualDate,
    string? OfficialOrganizer,
    string? Category,
    string? HistorySummary,
    string? HistorySource,
    string CurrentYearLabel,
    string? ThemeAr,
    string? ThemeEn,
    string? ThemeSourceUrl,
    IReadOnlyList<InternationalDayExportActivation> Activations,
    IReadOnlyList<string> Suggestions,
    IReadOnlyList<InternationalDayExportSource> Sources,
    string ExportedAtLabel);
