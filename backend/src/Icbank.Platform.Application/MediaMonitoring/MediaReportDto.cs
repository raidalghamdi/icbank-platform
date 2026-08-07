namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Read model for an editable, audience-tiered media-monitoring report (<c>media_reports</c>).</summary>
/// <param name="Id">The report id.</param>
/// <param name="Title">The report title.</param>
/// <param name="ReportType">The report cadence/type.</param>
/// <param name="Audience">The target audience tier.</param>
/// <param name="DateFrom">The UTC start of the covered date range.</param>
/// <param name="DateTo">The UTC end of the covered date range.</param>
/// <param name="Sources">The included source list.</param>
/// <param name="ExecutiveSummary">The AI-generated executive summary.</param>
/// <param name="ContentMd">The AI-generated markdown body.</param>
/// <param name="OverallTone">The overall AI-generated tone summary.</param>
/// <param name="Status">The lifecycle status.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
public sealed record MediaReportDto(
    int Id,
    string Title,
    string ReportType,
    string Audience,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    IReadOnlyList<string> Sources,
    string? ExecutiveSummary,
    string ContentMd,
    string? OverallTone,
    string Status,
    DateTimeOffset CreatedAt);
