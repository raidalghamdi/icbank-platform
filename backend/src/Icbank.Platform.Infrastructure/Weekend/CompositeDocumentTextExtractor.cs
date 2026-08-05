using System.Text;
using DocumentFormat.OpenXml.Packaging;
using Icbank.Platform.Application.Weekend;
using Icbank.Platform.Infrastructure.Rendering;
using UglyToad.PdfPig;

namespace Icbank.Platform.Infrastructure.Weekend;

/// <summary>
/// Real <see cref="IDocumentTextExtractor"/> implementation, replacing the Wave 1 placeholder
/// (<c>PlainTextDocumentTextExtractor</c>, which only handled <c>.txt</c>/<c>.md</c> and returned
/// an empty string for every other format, including ones it could have parsed). Ports the format
/// coverage of the Node source's <c>extractText</c> (<c>week-start.ts:39-90</c>) minus OCR: PDF via
/// PdfPig, DOCX via <c>DocumentFormat.OpenXml</c>, plain text passthrough for <c>text/*</c>. Images
/// are explicitly declined with <see cref="DocumentTextExtractionStatus.OcrNotSupported"/> rather
/// than silently returning empty text -- the Node source used GPT-4o vision OCR for images, which
/// is an external paid API this task's "no external API keys, pure libraries" constraint rules
/// out; see RENDERING-NOTES.md for the documented gap. Anything else is rejected with
/// <see cref="DocumentTextExtractionStatus.UnsupportedFormat"/>.
/// </summary>
public sealed class CompositeDocumentTextExtractor : IDocumentTextExtractor
{
    private const string NoTextReasonAr = "لا يوجد نص قابل للاستخراج";
    private const string OcrNotSupportedReasonAr = "استخراج النص من الصور (OCR) غير مدعوم في هذا الإصدار";
    private const string UnsupportedFormatReasonAr = "صيغة الملف غير مدعومة لاستخراج النص";
    private const string ParseFailedReasonAr = "تعذّرت قراءة محتوى الملف";
    private const string TooLargeReasonAr = "حجم الملف أكبر من الحد المسموح به لاستخراج النص";

    private static readonly string[] PlainTextExtensions = { ".txt", ".md" };
    private static readonly string[] PdfExtensions = { ".pdf" };
    private static readonly string[] DocxExtensions = { ".docx" };
    private static readonly string[] ImageExtensions = { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".bmp" };

    /// <inheritdoc />
    public async Task<DocumentTextExtractionResult> ExtractAsync(byte[] content, string contentType, string fileName, CancellationToken cancellationToken)
    {
        if (content.LongLength > RenderingGuard.MaxDocumentBytes)
        {
            return DocumentTextExtractionResult.Failure(DocumentTextExtractionStatus.InputTooLarge, TooLargeReasonAr);
        }

        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var normalizedContentType = (contentType ?? string.Empty).ToLowerInvariant();

        if (normalizedContentType.StartsWith("image/", StringComparison.Ordinal) || ImageExtensions.Contains(extension))
        {
            return DocumentTextExtractionResult.Failure(DocumentTextExtractionStatus.OcrNotSupported, OcrNotSupportedReasonAr);
        }

        if (PdfExtensions.Contains(extension) || normalizedContentType is "application/pdf")
        {
            return await ExtractPdfAsync(content, cancellationToken);
        }

        if (DocxExtensions.Contains(extension) || normalizedContentType is "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
        {
            return await ExtractDocxAsync(content, cancellationToken);
        }

        if (PlainTextExtensions.Contains(extension) || normalizedContentType.StartsWith("text/", StringComparison.Ordinal))
        {
            return ExtractPlainText(content);
        }

        return DocumentTextExtractionResult.Failure(DocumentTextExtractionStatus.UnsupportedFormat, UnsupportedFormatReasonAr);
    }

    private static DocumentTextExtractionResult ExtractPlainText(byte[] content)
    {
        // Why: a genuinely empty/whitespace-only text file is still a successful extraction (the
        // format was fully supported and fully read) -- the caller decides whether an empty
        // result means "nothing to archive", it is not this extractor's job to reclassify that
        // as a format failure.
        var text = Encoding.UTF8.GetString(content);
        return DocumentTextExtractionResult.Success(text);
    }

    private static async Task<DocumentTextExtractionResult> ExtractPdfAsync(byte[] content, CancellationToken cancellationToken)
    {
        try
        {
            return await RenderingGuard.RunWithTimeoutAsync(
                () =>
                {
                    using var document = PdfDocument.Open(content);
                    var builder = new StringBuilder();
                    foreach (UglyToad.PdfPig.Content.Page page in document.GetPages())
                    {
                        builder.AppendLine(page.Text);
                    }

                    return DocumentTextExtractionResult.Success(builder.ToString().Trim());
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not RenderingValidationException)
        {
            return DocumentTextExtractionResult.Failure(DocumentTextExtractionStatus.ParseFailed, ParseFailedReasonAr);
        }
    }

    private static async Task<DocumentTextExtractionResult> ExtractDocxAsync(byte[] content, CancellationToken cancellationToken)
    {
        try
        {
            return await RenderingGuard.RunWithTimeoutAsync(
                () =>
                {
                    using var stream = new MemoryStream(content, writable: false);
                    using var package = WordprocessingDocument.Open(stream, isEditable: false);
                    DocumentFormat.OpenXml.Wordprocessing.Body? body = package.MainDocumentPart?.Document?.Body;
                    if (body is null)
                    {
                        return DocumentTextExtractionResult.Success(string.Empty);
                    }

                    IEnumerable<string> textRuns = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text);
                    IEnumerable<string> paragraphs = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>()
                        .Select(p => string.Concat(p.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().Select(t => t.Text)))
                        .Where(p => p.Length > 0);
                    var combined = string.Join(Environment.NewLine, paragraphs);
                    return DocumentTextExtractionResult.Success(combined);
                },
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException and not RenderingValidationException)
        {
            return DocumentTextExtractionResult.Failure(DocumentTextExtractionStatus.ParseFailed, ParseFailedReasonAr);
        }
    }
}
