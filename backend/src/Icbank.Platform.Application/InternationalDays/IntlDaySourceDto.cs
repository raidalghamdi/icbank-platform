namespace Icbank.Platform.Application.InternationalDays;

/// <summary>Ports a single row of <c>intl_day_sources</c> (API-SURFACE.md §14).</summary>
/// <param name="Id">The source row id.</param>
/// <param name="SourceUrl">The source URL.</param>
/// <param name="SourceTitle">The source title.</param>
/// <param name="SourcePublisher">The source publisher.</param>
public sealed record IntlDaySourceDto(int Id, string? SourceUrl, string? SourceTitle, string? SourcePublisher);
