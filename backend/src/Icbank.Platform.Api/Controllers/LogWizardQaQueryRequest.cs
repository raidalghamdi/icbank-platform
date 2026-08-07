namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="FinalMediaReportsController.LogWizardQaQueryAsync"/>.</summary>
/// <param name="Period">The requested period, free text.</param>
/// <param name="Audience">The requested audience tier, free text.</param>
/// <param name="Sources">The requested source list.</param>
/// <param name="FocusTopics">The requested focus topics, free text.</param>
/// <param name="Language">The requested output language, free text.</param>
/// <param name="Recipients">The intended recipients, free text.</param>
/// <param name="Mode">The wizard mode: generate or search.</param>
public sealed record LogWizardQaQueryRequest(
    string? Period,
    string? Audience,
    IReadOnlyList<string>? Sources,
    string? FocusTopics,
    string? Language,
    string? Recipients,
    string? Mode);
