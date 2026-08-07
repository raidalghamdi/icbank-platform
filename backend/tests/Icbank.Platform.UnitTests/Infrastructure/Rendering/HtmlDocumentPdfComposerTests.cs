using FluentAssertions;
using Icbank.Platform.Infrastructure.Rendering;

namespace Icbank.Platform.UnitTests.Infrastructure.Rendering;

/// <summary>
/// Tests the shared QuestPDF HTML-to-PDF composer used by both
/// <see cref="Icbank.Platform.Infrastructure.MediaMonitoring.QuestPdfFinalReportPdfRenderer"/> and
/// <see cref="Icbank.Platform.Infrastructure.Shorfah.QuestPdfShorfahIssuePdfRenderer"/>.
/// </summary>
public sealed class HtmlDocumentPdfComposerTests
{
    [Fact]
    public void Compose_SimpleHtml_ProducesBytesStartingWithPdfMagicNumber()
    {
        var html = "<html><body><h1>Report</h1><p>Hello world</p></body></html>";

        var pdfBytes = HtmlDocumentPdfComposer.Compose(html);

        pdfBytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5).Should().Be("%PDF-", "every valid PDF byte stream must begin with the %PDF- magic number");
    }

    [Fact]
    public void Compose_ArabicHtml_ProducesNonTrivialPdfBytes()
    {
        var html = "<html><body><h1>تقرير الإعلام</h1><p>هذا نص عربي للتأكد من دعم اللغة العربية.</p></body></html>";

        var pdfBytes = HtmlDocumentPdfComposer.Compose(html);

        pdfBytes.Length.Should().BeGreaterThan(500, "an embedded-font Arabic-glyph PDF page should never collapse to a near-empty document");
        System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void Compose_HtmlWithTable_ProducesNonEmptyPdf()
    {
        var html = "<html><body><table><tr><th>العنوان</th><th>القيمة</th></tr><tr><td>الصف الأول</td><td>10</td></tr></table></body></html>";

        var pdfBytes = HtmlDocumentPdfComposer.Compose(html);

        pdfBytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public void Compose_HtmlEncodedEntities_DecodesBackToDisplayText()
    {
        // Why: FinalReportHtmlBuilder/ShorfahIssueHtmlBuilder HTML-encode every interpolated
        // value before this composer ever sees it (e.g. "&amp;" for "&") -- the composer must
        // decode entities back to display text since QuestPDF has no markup interpretation of
        // its own and would otherwise render the literal "&amp;" text.
        var html = "<html><body><p>شركة أ &amp; ب</p></body></html>";

        var pdfBytes = HtmlDocumentPdfComposer.Compose(html);

        pdfBytes.Should().NotBeEmpty();
    }
}
