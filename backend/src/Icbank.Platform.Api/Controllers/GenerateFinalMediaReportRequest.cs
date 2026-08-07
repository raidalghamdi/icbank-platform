namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="FinalMediaReportsController.GenerateAsync"/>.</summary>
/// <param name="PeriodLabel">The human-readable period label.</param>
/// <param name="Audience">The free-text target audience description.</param>
/// <param name="DateFrom">The range start.</param>
/// <param name="DateTo">The range end.</param>
/// <param name="FocusTopics">Optional focus-topics free text.</param>
/// <param name="Sources">
/// The source channels to include (<c>news</c>, <c>linkedin</c>, <c>twitter</c>). Omit to include
/// everything; older clients that never sent this field keep their previous behaviour.
/// </param>
public sealed record GenerateFinalMediaReportRequest(
    string PeriodLabel,
    string? Audience,
    DateTimeOffset DateFrom,
    DateTimeOffset DateTo,
    string? FocusTopics,
    IReadOnlyList<string>? Sources);
