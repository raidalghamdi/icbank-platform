namespace Icbank.Platform.Domain.Designs;

/// <summary>One list item with the icon chosen to illustrate it.</summary>
/// <param name="Icon">A catalogue icon name.</param>
/// <param name="Text">The item copy, already trimmed to the canvas budget.</param>
public sealed record IconEventBullet(string Icon, string Text);
