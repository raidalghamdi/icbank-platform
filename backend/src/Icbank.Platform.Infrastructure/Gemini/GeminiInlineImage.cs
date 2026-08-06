namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>One inline image part returned by an image-generation model call.</summary>
/// <param name="Base64Data">The base64-encoded raw image bytes.</param>
/// <param name="MimeType">The image MIME type, e.g. <c>image/png</c>.</param>
public sealed record GeminiInlineImage(string Base64Data, string MimeType);
