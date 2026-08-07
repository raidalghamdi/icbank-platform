namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>Result of running an AI Quick tool.</summary>
/// <param name="Output">The model's generated output text.</param>
/// <param name="Tool">The tool key that was run, echoed back for the caller's convenience.</param>
public sealed record RunQuickAiToolResultDto(string Output, string Tool);
