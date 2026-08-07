namespace Icbank.Platform.Application.Weekend;

/// <summary>
/// Port for extracting plain text from an uploaded document (BUSINESS-RULES.md §2.5's
/// PDF/DOCX/TXT/image-OCR pipeline). The Node source used <c>pdf-parse</c>, <c>mammoth</c>, and
/// GPT-4o vision OCR; this port keeps the actual parsing/OCR libraries out of Application
/// (R-BE-002).
/// </summary>
/// <remarks>
/// <b>Interface change from the original port (documented per task instructions):</b> the return
/// type was changed from <c>Task&lt;string&gt;</c> to <see cref="Task{DocumentTextExtractionResult}"/>.
/// The original shape could not distinguish "genuinely empty text file" from "unsupported binary
/// format" from "image requiring OCR we do not perform" -- all three used to collapse to the same
/// empty string, which the caller then reported as one generic "no extractable text" skip reason.
/// The real implementation must reject unsupported formats with a clear reason and must not
/// silently return empty text for formats it cannot handle (task requirement), which needs a
/// richer result shape to express honestly.
/// </remarks>
public interface IDocumentTextExtractor
{
    /// <summary>Extracts plain text from a document's raw bytes.</summary>
    /// <param name="content">The raw file bytes.</param>
    /// <param name="contentType">The uploaded MIME content type.</param>
    /// <param name="fileName">The original file name (used to infer format when the content type is generic).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extraction outcome: success with text, or a specific, explained failure/decline status.</returns>
    Task<DocumentTextExtractionResult> ExtractAsync(byte[] content, string contentType, string fileName, CancellationToken cancellationToken);
}
