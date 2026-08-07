namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>A single uploaded document (raw bytes plus multipart metadata).</summary>
/// <param name="FileName">The original file name.</param>
/// <param name="ContentType">The uploaded MIME content type.</param>
/// <param name="Content">The raw file bytes.</param>
public sealed record UploadedDocument(string FileName, string ContentType, byte[] Content);
