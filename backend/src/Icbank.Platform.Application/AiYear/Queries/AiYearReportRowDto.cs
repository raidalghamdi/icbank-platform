namespace Icbank.Platform.Application.AiYear.Queries;

/// <summary>One report row.</summary>
/// <param name="Title">The activation title.</param>
/// <param name="Month">The calendar month (1-12).</param>
/// <param name="Type">The activation type.</param>
/// <param name="Channels">The distribution channels, joined for display.</param>
/// <param name="Reach">The reach metric, if recorded.</param>
public sealed record AiYearReportRowDto(string Title, int Month, string Type, IReadOnlyList<string> Channels, int? Reach);
