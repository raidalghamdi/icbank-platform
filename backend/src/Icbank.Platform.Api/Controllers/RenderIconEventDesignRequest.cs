namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <see cref="IconEventDesignsController.RenderAsync"/>.</summary>
/// <param name="Html">The HTML document to rasterize.</param>
/// <param name="Size">The target size preset key.</param>
/// <param name="Quality">The render quality, <c>hd</c> or <c>ultra</c>.</param>
public sealed record RenderIconEventDesignRequest(string Html, string Size, string? Quality);
