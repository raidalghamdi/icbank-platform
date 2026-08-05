namespace Icbank.Platform.Application.Weekend;

/// <summary>The outcome kind of a <see cref="IDocumentTextExtractor"/> call.</summary>
public enum DocumentTextExtractionStatus
{
    /// <summary>Text was extracted successfully (possibly empty if the document genuinely has no text).</summary>
    Success,

    /// <summary>The content type/extension is not one this extractor supports (e.g. an unknown binary format).</summary>
    UnsupportedFormat,

    /// <summary>The content type is an image; this extractor performs no OCR and explicitly declines rather than returning empty text silently.</summary>
    OcrNotSupported,

    /// <summary>The input exceeded the extractor's size cap and was rejected before parsing.</summary>
    InputTooLarge,

    /// <summary>The input was recognized but could not be parsed (corrupt/malformed document).</summary>
    ParseFailed,
}

/// <summary>
/// The result of a <see cref="IDocumentTextExtractor.ExtractAsync"/> call. Replaces the placeholder
/// port's bare <c>Task&lt;string&gt;</c> return type -- that shape could not distinguish "this is a
/// text file with no content" from "this is a PDF we chose not to parse" from "this is an image we
/// cannot OCR", which the task's clear-validation-error and explicit-non-OCR requirements need to
/// express. This is the one documented, deliberate port-interface change described in
/// RENDERING-NOTES.md.
/// </summary>
/// <param name="Status">The outcome kind.</param>
/// <param name="Text">The extracted text, populated only when <see cref="Status"/> is <see cref="DocumentTextExtractionStatus.Success"/>.</param>
/// <param name="Reason">A human-readable (Arabic, matching the existing UI copy) explanation, populated for every non-success status.</param>
public sealed record DocumentTextExtractionResult(DocumentTextExtractionStatus Status, string? Text, string? Reason)
{
    /// <summary>Builds a successful result.</summary>
    /// <param name="text">The extracted text.</param>
    /// <returns>A <see cref="DocumentTextExtractionResult"/> with <see cref="DocumentTextExtractionStatus.Success"/>.</returns>
    public static DocumentTextExtractionResult Success(string text) => new(DocumentTextExtractionStatus.Success, text, null);

    /// <summary>Builds a failed/declined result.</summary>
    /// <param name="status">The specific non-success status.</param>
    /// <param name="reason">A human-readable explanation.</param>
    /// <returns>A <see cref="DocumentTextExtractionResult"/> carrying the given status and reason.</returns>
    public static DocumentTextExtractionResult Failure(DocumentTextExtractionStatus status, string reason) => new(status, null, reason);
}
