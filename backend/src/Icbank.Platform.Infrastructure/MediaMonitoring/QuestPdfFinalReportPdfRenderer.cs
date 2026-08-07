using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Infrastructure.Rendering;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Real <see cref="IFinalReportPdfRenderer"/> implementation, replacing the Wave 3a placeholder
/// (<c>TemplateFinalReportPdfRenderer</c>, which returned the input HTML bytes unchanged with a
/// spoofed <c>application/pdf</c> content type). Renders the already-HTML-encoded document (see
/// <see cref="FinalReportHtmlBuilder"/>) to a true PDF byte stream via QuestPDF (Community license
/// -- see <c>RENDERING-NOTES.md</c>) and <see cref="HtmlDocumentPdfComposer"/>, with an embedded
/// GAC-approved Frutiger LT Arabic font, providing full Arabic glyph coverage and right-to-left layout so the
/// container never depends on a system font being present.
/// </summary>
public sealed class QuestPdfFinalReportPdfRenderer : IFinalReportPdfRenderer
{
    /// <inheritdoc />
    public async Task<byte[]> RenderAsync(string html, CancellationToken cancellationToken)
    {
        RenderingGuard.EnsureWithinLimit(System.Text.Encoding.UTF8.GetByteCount(html), "Final report HTML input");
        var pdfBytes = await RenderingGuard.RunWithTimeoutAsync(() => HtmlDocumentPdfComposer.Compose(html), cancellationToken);
        RenderingGuard.EnsureWithinLimit(pdfBytes.LongLength, "Rendered final report PDF");
        return pdfBytes;
    }
}
