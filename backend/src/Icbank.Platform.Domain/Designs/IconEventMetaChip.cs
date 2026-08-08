namespace Icbank.Platform.Domain.Designs;

/// <summary>One small labelled chip such as a date, a time, a place or a contact.</summary>
/// <param name="Icon">A catalogue icon name.</param>
/// <param name="Value">The literal value, never reformatted.</param>
public sealed record IconEventMetaChip(string Icon, string Value);
