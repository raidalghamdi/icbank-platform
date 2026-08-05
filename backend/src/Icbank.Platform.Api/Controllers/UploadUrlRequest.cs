namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for logo/font upload-URL endpoints.</summary>
/// <param name="FileName">The client-supplied original file name, used only to derive a safe extension.</param>
/// <param name="ContentType">The optional MIME content type.</param>
public sealed record UploadUrlRequest(string FileName, string? ContentType);
