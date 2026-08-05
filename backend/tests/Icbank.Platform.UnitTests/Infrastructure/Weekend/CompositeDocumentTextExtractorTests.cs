using System.Text;
using FluentAssertions;
using Icbank.Platform.Application.Weekend;
using Icbank.Platform.Infrastructure.Rendering;
using Icbank.Platform.Infrastructure.Shorfah;
using Icbank.Platform.Infrastructure.Weekend;

namespace Icbank.Platform.UnitTests.Infrastructure.Weekend;

/// <summary>
/// Tests the real <see cref="IDocumentTextExtractor"/> implementation covering PDF (PdfPig),
/// DOCX (OpenXml), plain-text passthrough, explicit image-OCR decline, and unsupported-format
/// rejection.
/// </summary>
public sealed class CompositeDocumentTextExtractorTests
{
    private readonly CompositeDocumentTextExtractor _extractor = new();

    [Fact]
    public async Task ExtractAsync_PlainTextFile_ReturnsSuccessWithExactText()
    {
        const string expectedText = "Hello, this is a plain text fixture.";
        var bytes = Encoding.UTF8.GetBytes(expectedText);

        DocumentTextExtractionResult result = await _extractor.ExtractAsync(bytes, "text/plain", "note.txt", CancellationToken.None);

        result.Status.Should().Be(DocumentTextExtractionStatus.Success);
        result.Text.Should().Be(expectedText);
    }

    [Fact]
    public async Task ExtractAsync_ArabicPlainTextFile_RoundTripsArabicTextExactly()
    {
        const string expectedText = "هذا نص عربي للتأكد من سلامة استخراج النص.";
        var bytes = Encoding.UTF8.GetBytes(expectedText);

        DocumentTextExtractionResult result = await _extractor.ExtractAsync(bytes, "text/plain", "arabic.txt", CancellationToken.None);

        result.Status.Should().Be(DocumentTextExtractionStatus.Success);
        result.Text.Should().Be(expectedText);
    }

    [Fact]
    public async Task ExtractAsync_DocxFixture_ExtractsArabicTextFromWordDocumentXml()
    {
        // Why: builds a real fixture via the OpenXml DOCX renderer rather than a hand-crafted zip
        // -- this both provides a realistic .docx and exercises the OpenXml Descendants<Text>
        // walk against genuine Word markup (runs split across multiple <w:t> elements etc.).
        var docxRenderer = new OpenXmlShorfahDocxRenderer();
        var docxBytes = await docxRenderer.RenderAsync("عنوان النشرة", "الفقرة الأولى من النص.\n\nالفقرة الثانية من النص.", CancellationToken.None);

        DocumentTextExtractionResult result = await _extractor.ExtractAsync(
            docxBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "issue.docx", CancellationToken.None);

        result.Status.Should().Be(DocumentTextExtractionStatus.Success);
        result.Text.Should().Contain("عنوان النشرة");
        result.Text.Should().Contain("الفقرة الأولى من النص");
        result.Text.Should().Contain("الفقرة الثانية من النص");
    }

    [Fact]
    public async Task ExtractAsync_PdfFixture_ExtractsEnglishTextViaPdfPig()
    {
        // Why: QuestPDF-rendered Arabic text extracted back out via PdfPig is not guaranteed to
        // preserve visual (or even logical) character order -- PDF text extraction reconstructs
        // runs from positioned glyphs, and RTL shaping/bidi reordering is a known extraction
        // fidelity gap across PDF tooling in general, not specific to this stack. This is
        // documented in RENDERING-NOTES.md. The reliable round-trip assertion here therefore uses
        // LTR (English) text, where PdfPig's glyph-order reconstruction is unambiguous.
        var html = "<html><body><h1>Fixture Report</h1><p>The quick brown fox jumps over the lazy dog.</p></body></html>";
        var pdfBytes = HtmlDocumentPdfComposer.Compose(html);

        DocumentTextExtractionResult result = await _extractor.ExtractAsync(pdfBytes, "application/pdf", "fixture.pdf", CancellationToken.None);

        result.Status.Should().Be(DocumentTextExtractionStatus.Success);
        result.Text.Should().Contain("Fixture Report");
        result.Text.Should().Contain("quick brown fox");
    }

    [Fact]
    public async Task ExtractAsync_ArabicPdfFixture_ExtractsNonEmptyTextContainingArabicCharacters()
    {
        var html = "<html><body><p>هذا نص عربي</p></body></html>";
        var pdfBytes = HtmlDocumentPdfComposer.Compose(html);

        DocumentTextExtractionResult result = await _extractor.ExtractAsync(pdfBytes, "application/pdf", "arabic.pdf", CancellationToken.None);

        result.Status.Should().Be(DocumentTextExtractionStatus.Success);
        result.Text.Should().NotBeNullOrWhiteSpace();

        // Why: this assertion documents a real, verified fidelity gap rather than an idealized
        // round trip. PdfPig reconstructs text from positioned glyphs and does not undo Arabic
        // presentation-form shaping or RTL bidi reordering, so "هذا نص عربي" survives extraction
        // as non-empty Arabic-script text, but neither the original code points nor their order
        // are guaranteed to match exactly. This was confirmed empirically against this exact
        // fixture (extraction produced per-word-reversed presentation-form glyphs) and is called
        // out in RENDERING-NOTES.md as a known caveat: no pure, no-API-key .NET library performs
        // Arabic bidi/shape reversal on PdfPig's glyph output. The extraction is still "real"
        // (non-empty, format-correct, no silent failure) -- it just cannot promise logical-order
        // or code-point fidelity for RTL scripts specifically, unlike the DOCX/plain-text paths
        // above which round-trip Arabic exactly.
        var isArabicScript = result.Text!.Any(c => c is (>= '\u0600' and <= '\u06FF') or (>= '\uFB50' and <= '\uFEFF'));
        isArabicScript.Should().BeTrue("the extracted text must still contain Arabic-range Unicode characters even though exact shaping/order is not guaranteed");
    }

    [Fact]
    public async Task ExtractAsync_ImageContentType_ReturnsExplicitOcrNotSupportedNotEmptyText()
    {
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 };

        DocumentTextExtractionResult result = await _extractor.ExtractAsync(bytes, "image/png", "photo.png", CancellationToken.None);

        result.Status.Should().Be(DocumentTextExtractionStatus.OcrNotSupported);
        result.Text.Should().BeNull();
        result.Reason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task ExtractAsync_UnknownBinaryFormat_ReturnsUnsupportedFormatNotEmptyText()
    {
        var bytes = new byte[] { 0x00, 0x01, 0x02, 0x03 };

        DocumentTextExtractionResult result = await _extractor.ExtractAsync(bytes, "application/x-msdownload", "app.exe", CancellationToken.None);

        result.Status.Should().Be(DocumentTextExtractionStatus.UnsupportedFormat);
        result.Text.Should().BeNull();
    }

    [Fact]
    public async Task ExtractAsync_OversizedInput_ReturnsInputTooLargeWithoutParsing()
    {
        var oversized = new byte[RenderingGuard.MaxDocumentBytes + 1];

        DocumentTextExtractionResult result = await _extractor.ExtractAsync(oversized, "text/plain", "huge.txt", CancellationToken.None);

        result.Status.Should().Be(DocumentTextExtractionStatus.InputTooLarge);
    }

    [Fact]
    public async Task ExtractAsync_CorruptPdfBytes_ReturnsParseFailedNotUnhandledException()
    {
        var corrupt = Encoding.ASCII.GetBytes("%PDF-1.4 this is not a real pdf body");

        DocumentTextExtractionResult result = await _extractor.ExtractAsync(corrupt, "application/pdf", "corrupt.pdf", CancellationToken.None);

        result.Status.Should().Be(DocumentTextExtractionStatus.ParseFailed);
    }
}
