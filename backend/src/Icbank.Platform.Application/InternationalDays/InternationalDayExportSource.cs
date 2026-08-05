namespace Icbank.Platform.Application.InternationalDays;

/// <summary>One source citation row rendered by <see cref="InternationalDayHtmlExportBuilder"/>.</summary>
/// <param name="SourceUrl">The source URL.</param>
/// <param name="SourceTitle">The source title.</param>
/// <param name="SourcePublisher">The source publisher.</param>
public sealed record InternationalDayExportSource(string? SourceUrl, string? SourceTitle, string? SourcePublisher);
