namespace Icbank.Platform.Application.Designs.IconEvent;

/// <summary>The typed shape of one extracted statistic entry (H-2 typed AI JSON).</summary>
/// <param name="Icon">The AI-selected icon name.</param>
/// <param name="Value">The literal numeric/text value.</param>
/// <param name="Label">The contextual label.</param>
public sealed record IconEventStatDto(string Icon, string Value, string Label);
