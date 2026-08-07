namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Port for rendering a final report's HTML representation to PDF bytes
/// (<c>POST /final-media-reports/:id/export-pdf</c>). The Node source used headless
/// Chromium/Puppeteer; this port keeps the rendering engine out of Application (R-BE-002) and
/// swappable in Infrastructure. Wave 3a ships a placeholder that returns the UTF-8 bytes of the
/// already-encoded HTML (see <see cref="FinalReportHtmlBuilder"/>) rather than true PDF bytes --
/// wiring a real headless-browser/PDF engine is deferred, see WAVE3A-PORT-NOTES.md.
/// </summary>
public interface IFinalReportPdfRenderer
{
    /// <summary>Renders the given HTML-encoded document to PDF bytes.</summary>
    /// <param name="html">The fully HTML-encoded report document (see <see cref="FinalReportHtmlBuilder"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered document bytes.</returns>
    Task<byte[]> RenderAsync(string html, CancellationToken cancellationToken);
}
