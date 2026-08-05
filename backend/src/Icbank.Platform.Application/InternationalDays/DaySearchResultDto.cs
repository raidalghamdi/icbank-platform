namespace Icbank.Platform.Application.InternationalDays;

/// <summary>
/// The full typed shape of the AI search prompt's JSON response (BUSINESS-RULES.md §4.2's
/// verbatim prompt schema). Closes DEFECT-LOG.md DATA-04/H-2: AI output is deserialized into this
/// type and validated by <see cref="DaySearchResultValidator"/> before any part of it reaches
/// <c>POST /intl-days/save</c>'s persistence step -- the Node source trusted the parsed JSON
/// directly with no schema validation.
/// </summary>
/// <param name="DayNameAr">The Arabic day name.</param>
/// <param name="DayNameEn">The English day name.</param>
/// <param name="AnnualDate">The free-text annual date, e.g. "21 مارس".</param>
/// <param name="OfficialOrganizer">The official international sponsoring body.</param>
/// <param name="OfficialOrganizerSource">The source URL for the organizer claim, or <c>null</c>.</param>
/// <param name="HistorySummary">A short historical summary.</param>
/// <param name="HistorySource">The source URL for the history summary, or <c>null</c>.</param>
/// <param name="CurrentThemeAr">The current year's Arabic theme.</param>
/// <param name="CurrentThemeEn">The current year's English theme.</param>
/// <param name="ThemeSourceUrl">The source URL for the theme, or <c>null</c>.</param>
/// <param name="Activations">The 8-15 required Saudi-entity activations spanning the 3-year window (BUSINESS-RULES.md §4.2).</param>
/// <param name="DesignSamples">The 3-5 required visual design samples.</param>
/// <param name="Suggestions">At least 5 suggested activation ideas.</param>
/// <param name="Sources">The cited sources.</param>
public sealed record DaySearchResultDto(
    string? DayNameAr,
    string? DayNameEn,
    string? AnnualDate,
    string? OfficialOrganizer,
    string? OfficialOrganizerSource,
    string? HistorySummary,
    string? HistorySource,
    string? CurrentThemeAr,
    string? CurrentThemeEn,
    string? ThemeSourceUrl,
    IReadOnlyList<DaySearchActivationDto>? Activations,
    IReadOnlyList<DaySearchDesignSampleDto>? DesignSamples,
    IReadOnlyList<string>? Suggestions,
    IReadOnlyList<DaySearchSourceDto>? Sources);
