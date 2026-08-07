namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>One dynamic variable entry of a <see cref="PromptFrameworkDto"/>.</summary>
/// <param name="Key">The variable key used in <c>{{key}}</c> placeholders.</param>
/// <param name="Label">The human-readable label.</param>
/// <param name="Type">The optional variable data type hint.</param>
/// <param name="Required">Whether the variable is required.</param>
public sealed record PromptVariableDto(string Key, string Label, string? Type, bool? Required);
