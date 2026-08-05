namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>The response shape for <c>POST /designs/render</c>.</summary>
/// <param name="Url">The object path of the composed image.</param>
public sealed record RenderDesignResultDto(string Url);
