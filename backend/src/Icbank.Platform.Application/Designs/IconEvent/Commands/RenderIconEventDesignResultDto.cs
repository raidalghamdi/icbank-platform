namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>The response shape for <c>POST /designs/icon-event/render</c>.</summary>
/// <param name="Url">The object path of the saved rendered image.</param>
/// <param name="Size">The size preset key.</param>
/// <param name="Width">The rendered pixel width, scaled by the device scale factor.</param>
/// <param name="Height">The rendered pixel height, scaled by the device scale factor.</param>
/// <param name="Quality">The resolved quality label, e.g. <c>hd (3x)</c>.</param>
public sealed record RenderIconEventDesignResultDto(string Url, string Size, int Width, int Height, string Quality);
