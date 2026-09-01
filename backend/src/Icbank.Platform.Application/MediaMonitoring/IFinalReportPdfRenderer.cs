namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Port for rendering a final report's HTML representation to PDF bytes
/// (<c>POST /final-media-reports/:id/export-pdf</c>). The Node source used headless
/// Chromium/Puppeteer; this port keeps the rendering engine out of Application (R-BE-002) and
/// swappable in Infrastructure.
/// </summary>
public interface IFinalReportPdfRenderer
{
    /// <summary>Renders the given HTML-encoded document to PDF bytes.</summary>
    /// <param name="html">The fully HTML-encoded report document (see <see cref="FinalReportHtmlBuilder"/>).</param>
    /// <param name="footerLabel">The label printed beside the page number on every page.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered document bytes.</returns>
    Task<byte[]> RenderAsync(string html, string? footerLabel, CancellationToken cancellationToken);
}
