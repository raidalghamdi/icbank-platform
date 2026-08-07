using Icbank.Platform.Application.MediaMonitoring.Commands;

namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="FinalMediaReportsController.CreateAsync"/>.</summary>
/// <param name="Title">The report title.</param>
/// <param name="ReportType">The report cadence/type key.</param>
/// <param name="PeriodLabel">The human-readable period label.</param>
/// <param name="DateFrom">The UTC start of the covered date range.</param>
/// <param name="DateTo">The UTC end of the covered date range.</param>
/// <param name="Draft">The 8-section content to persist.</param>
public sealed record CreateFinalMediaReportRequest(
    string Title, string? ReportType, string PeriodLabel, DateTimeOffset DateFrom, DateTimeOffset DateTo, FinalReportDraftDto Draft);
