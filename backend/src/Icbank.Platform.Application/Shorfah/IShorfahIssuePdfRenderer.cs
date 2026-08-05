namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Port for rendering an issue's HTML representation to PDF bytes
/// (<c>GET /shorfah/issues/:id/pdf.pdf</c>). The Node source used headless Chromium/Puppeteer,
/// with a graceful HTML+auto-print fallback if Chromium failed to launch (BUSINESS-RULES.md
/// §1.9). This port keeps the rendering engine out of Application (R-BE-002) and swappable in
/// Infrastructure, following the exact same narrow-named-interface + deterministic-placeholder
/// pattern as Wave 3a's <c>IFinalReportPdfRenderer</c>/<c>TemplateFinalReportPdfRenderer</c> (read
/// before writing this port, per the task's explicit instruction).
/// </summary>
public interface IShorfahIssuePdfRenderer
{
    /// <summary>Renders the given HTML-encoded document to PDF bytes.</summary>
    /// <param name="html">The fully HTML-encoded issue document (see <see cref="ShorfahIssueHtmlBuilder"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered document bytes.</returns>
    Task<byte[]> RenderAsync(string html, CancellationToken cancellationToken);
}
