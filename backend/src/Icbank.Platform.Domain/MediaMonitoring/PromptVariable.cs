namespace Icbank.Platform.Domain.MediaMonitoring;

/// <summary>One entry of <c>prompt_frameworks.variables</c> (DATA-MODEL.md section 6).</summary>
public sealed class PromptVariable
{
    /// <summary>Gets or sets the variable key used in <c>{{key}}</c> placeholders.</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Gets or sets the human-readable label.</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets the optional variable data type hint.</summary>
    public string? Type { get; set; }

    /// <summary>Gets or sets a value indicating whether the variable is required.</summary>
    public bool? Required { get; set; }
}
