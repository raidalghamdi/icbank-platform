namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>One generated background-image result.</summary>
/// <param name="Content">The raw image bytes.</param>
/// <param name="ContentType">The MIME content type.</param>
public sealed record GeneratedBackgroundImage(byte[] Content, string ContentType);
