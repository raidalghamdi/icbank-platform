namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>One rendered size variant from the studio endpoint.</summary>
/// <param name="Size">The size preset key.</param>
/// <param name="Width">The pixel width.</param>
/// <param name="Height">The pixel height.</param>
/// <param name="Html">The rendered HTML document.</param>
public sealed record IconEventStudioVariantDto(string Size, int Width, int Height, string Html);
