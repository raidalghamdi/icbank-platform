namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="MediaMonitoringController.RunPromptAsync"/>.</summary>
/// <param name="Variables">The variable substitution map.</param>
public sealed record RunPromptFrameworkRequest(IReadOnlyDictionary<string, string>? Variables);
