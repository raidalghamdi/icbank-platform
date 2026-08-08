namespace Icbank.Platform.Domain.Designs;

/// <summary>Describes one icon-event output size.</summary>
/// <param name="WireValue">The hyphenated identifier clients send, for example <c>web-standard</c>.</param>
/// <param name="Width">The canvas width in pixels.</param>
/// <param name="Height">The canvas height in pixels.</param>
/// <param name="AspectLabel">The human-readable aspect ratio, for example <c>16:9 UHD</c>.</param>
/// <param name="EnglishLabel">The English display name.</param>
/// <param name="ArabicLabel">The Arabic display name shown in the designer.</param>
public sealed record IconEventSizeSpec(
    string WireValue,
    int Width,
    int Height,
    string AspectLabel,
    string EnglishLabel,
    string ArabicLabel);
