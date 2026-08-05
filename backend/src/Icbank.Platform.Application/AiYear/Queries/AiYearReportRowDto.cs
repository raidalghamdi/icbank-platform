namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>One report row.</summary>
/// <param name="Title">The activation title.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="MonthNameAr">
/// The Arabic month name for <paramref name="Month"/>, matching the Node source's
/// <c>MONTHS_AR[a.month - 1]</c> lookup (<c>ai-year.ts:460,478</c>) -- this closes the gap
/// WAVE2-PORT-NOTES.md flagged ("a future DOCX-writer consuming <c>AiYearReportRowDto.Month</c> as
/// a raw <c>int</c> will need to do its own Arabic month-name mapping") by resolving it once,
/// here, so every consumer of this DTO gets the display name for free.
/// </param>
/// <param name="Type">The activation type.</param>
/// <param name="Channels">The distribution channels, joined for display.</param>
/// <param name="Reach">The reach metric, if recorded.</param>
public sealed record AiYearReportRowDto(string Title, int Month, string MonthNameAr, string Type, IReadOnlyList<string> Channels, int? Reach);
