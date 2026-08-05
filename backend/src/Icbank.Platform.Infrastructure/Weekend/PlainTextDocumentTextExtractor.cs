using System.Text;
using Icbank.Platform.Application.Weekend;

namespace Icbank.Platform.Infrastructure.Weekend;

/// <summary>
/// Default <see cref="IDocumentTextExtractor"/> implementation. The Node source used
/// <c>pdf-parse</c>, <c>mammoth</c>, and GPT-4o vision OCR for PDF/DOCX/image formats
/// respectively (BUSINESS-RULES.md §2.5); this port implements the plain-text/Markdown path only
/// (UTF-8 decode) and returns empty text for PDF/DOCX/image inputs, matching the Node source's
/// own fallback ("return \"\"" for unrecognized formats) rather than silently mis-extracting —
/// wiring real PDF/DOCX parsers and an OCR provider is deferred, see WAVE1-PORT-NOTES.md.
/// </summary>
public sealed class PlainTextDocumentTextExtractor : IDocumentTextExtractor
{
    private static readonly string[] PlainTextExtensions = { ".txt", ".md" };

    /// <inheritdoc />
    public Task<string> ExtractAsync(byte[] content, string contentType, string fileName, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (PlainTextExtensions.Contains(extension))
        {
            return Task.FromResult(Encoding.UTF8.GetString(content));
        }

        return Task.FromResult(string.Empty);
    }
}
