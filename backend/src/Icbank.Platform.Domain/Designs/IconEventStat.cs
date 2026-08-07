namespace Icbank.Platform.Domain.Designs;

/// <summary>One extracted statistic chip for an icon-event design (BUSINESS-RULES.md §7.4).</summary>
/// <param name="Icon">The icon name (must be a valid <see cref="IconLibrary"/> entry).</param>
/// <param name="Value">The literal value as it appeared in the source text, e.g. <c>"135+"</c>.</param>
/// <param name="Label">The contextual label describing what the value represents.</param>
public sealed record IconEventStat(string Icon, string Value, string Label);
