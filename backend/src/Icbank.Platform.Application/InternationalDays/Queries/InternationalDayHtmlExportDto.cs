namespace Icbank.Platform.Application.InternationalDays.Queries;

/// <summary>The rendered export document.</summary>
/// <param name="FileNameWithoutExtension">The day's Arabic name, used to build the download file name.</param>
/// <param name="Html">The fully HTML-encoded export document.</param>
public sealed record InternationalDayHtmlExportDto(string FileNameWithoutExtension, string Html);
