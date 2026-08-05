namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Port for rendering an issue to a Word-compatible document
/// (<c>GET /shorfah/issues/:id/docx</c>). The Node source used the <c>docx</c> npm package to
/// build a real <c>.docx</c> OOXML binary with Arabic RTL styling. Standing up an OOXML-writing
/// dependency in this port is deferred, see WAVE4A-PORT-NOTES.md -- this port keeps the rendering
/// engine out of Application (R-BE-002) and swappable in Infrastructure, following the exact
/// same narrow-named-interface + deterministic-placeholder pattern as Wave 3a's
/// <c>IFinalReportPdfRenderer</c>/<c>TemplateFinalReportPdfRenderer</c>.
/// </summary>
public interface IShorfahDocxRenderer
{
    /// <summary>Renders the given plain-text document body (already markdown-stripped, matching the Node source's <c>stripMd()</c>) to document bytes.</summary>
    /// <param name="titleAr">The document title.</param>
    /// <param name="plainTextBody">The already markdown-stripped plain-text body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered document bytes.</returns>
    Task<byte[]> RenderAsync(string titleAr, string plainTextBody, CancellationToken cancellationToken);
}
