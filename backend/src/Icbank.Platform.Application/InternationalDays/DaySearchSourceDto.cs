namespace Icbank.Platform.Application.InternationalDays;

/// <summary>One AI-returned source citation from the search prompt's <c>sources</c> array (BUSINESS-RULES.md §4.2).</summary>
/// <param name="Url">The source URL.</param>
/// <param name="Title">The source title.</param>
/// <param name="Publisher">The publisher name.</param>
public sealed record DaySearchSourceDto(string? Url, string? Title, string? Publisher);
