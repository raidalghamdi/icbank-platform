namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="MediaMonitoringController.GenerateReportAsync"/>.</summary>
/// <param name="Audience">The target audience tier key.</param>
/// <param name="ReportType">The report cadence/type key.</param>
/// <param name="DateFrom">The optional explicit range start.</param>
/// <param name="DateTo">The optional explicit range end.</param>
/// <param name="Sources">The source list to include.</param>
/// <param name="CustomTitle">An optional caller-supplied title override.</param>
public sealed record GenerateMediaReportRequest(
    string? Audience,
    string? ReportType,
    DateTimeOffset? DateFrom,
    DateTimeOffset? DateTo,
    IReadOnlyList<string>? Sources,
    string? CustomTitle);
