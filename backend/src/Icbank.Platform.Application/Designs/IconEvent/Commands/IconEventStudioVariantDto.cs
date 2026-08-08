namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>One rendered size variant from the studio endpoint.</summary>
/// <param name="Size">The size preset wire value, for example <c>web-standard</c>.</param>
/// <param name="Width">The pixel width.</param>
/// <param name="Height">The pixel height.</param>
/// <param name="AspectLabel">The aspect ratio label, for example <c>16:9 UHD</c>.</param>
/// <param name="Label">The Arabic display name shown beside the rendered preview.</param>
/// <param name="Html">The rendered HTML document.</param>
public sealed record IconEventStudioVariantDto(
    string Size,
    int Width,
    int Height,
    string AspectLabel,
    string Label,
    string Html);
