namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Result of a <see cref="RegenerateExecutiveSummaryCommand"/>.</summary>
/// <param name="Summary">The regenerated executive-summary Markdown text.</param>
/// <param name="ReportNumber">The report's official number, echoed back for the caller's convenience.</param>
public sealed record RegenerateExecutiveSummaryResultDto(string Summary, string ReportNumber);
