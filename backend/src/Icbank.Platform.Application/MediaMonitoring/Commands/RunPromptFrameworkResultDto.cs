namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Result of executing a prompt framework.</summary>
/// <param name="Output">The model's generated output text.</param>
/// <param name="PromptSent">The fully-substituted prompt text that was sent to the model.</param>
public sealed record RunPromptFrameworkResultDto(string Output, string PromptSent);
