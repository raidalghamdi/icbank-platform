namespace Icbank.Platform.Application.Weekend;

/// <summary>
/// Port for extracting plain text from an uploaded document (BUSINESS-RULES.md §2.5's
/// PDF/DOCX/TXT/image-OCR pipeline). The Node source used <c>pdf-parse</c>, <c>mammoth</c>, and
/// GPT-4o vision OCR; this port keeps the actual parsing/OCR libraries out of Application
/// (R-BE-002).
/// </summary>
public interface IDocumentTextExtractor
{
    /// <summary>Extracts plain text from a document's raw bytes.</summary>
    /// <param name="content">The raw file bytes.</param>
    /// <param name="contentType">The uploaded MIME content type.</param>
    /// <param name="fileName">The original file name (used to infer format when the content type is generic).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The extracted plain text, or an empty string if no text could be extracted.</returns>
    Task<string> ExtractAsync(byte[] content, string contentType, string fileName, CancellationToken cancellationToken);
}
