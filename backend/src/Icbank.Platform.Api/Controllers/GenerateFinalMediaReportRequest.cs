namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="FinalMediaReportsController.GenerateAsync"/>.</summary>
/// <param name="PeriodLabel">The human-readable period label.</param>
/// <param name="Audience">The free-text target audience description.</param>
/// <param name="DateFrom">The range start.</param>
/// <param name="DateTo">The range end.</param>
/// <param name="FocusTopics">Optional focus-topics free text.</param>
public sealed record GenerateFinalMediaReportRequest(
    string PeriodLabel, string? Audience, DateTimeOffset DateFrom, DateTimeOffset DateTo, string? FocusTopics);
