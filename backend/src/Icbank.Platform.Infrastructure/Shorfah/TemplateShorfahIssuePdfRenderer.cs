using System.Text;
using Icbank.Platform.Application.Shorfah;

namespace Icbank.Platform.Infrastructure.Shorfah;

/// <summary>
/// Deterministic, non-Chromium default <see cref="IShorfahIssuePdfRenderer"/> implementation. The
/// Node source used headless Chromium/Puppeteer (with an HTML+auto-print fallback if Chromium
/// failed to launch) to rasterize <see cref="ShorfahIssueHtmlBuilder"/>'s HTML output into a true
/// PDF byte stream; wiring a real headless-browser/PDF engine is deferred for Wave 4a (see
/// WAVE4A-PORT-NOTES.md), following the exact same deferral pattern as Wave 3a's
/// <c>TemplateFinalReportPdfRenderer</c>. This implementation returns the UTF-8 bytes of the
/// already-encoded HTML document unchanged, so the binary-PDF-download endpoint is fully
/// exercisable end-to-end (persistence, authorization, section selection, audit-worthiness)
/// without an external rendering dependency. The returned bytes are intentionally HTML, not a
/// binary PDF; callers must not assume a <c>%PDF-</c> magic number until a real engine is wired.
/// </summary>
public sealed class TemplateShorfahIssuePdfRenderer : IShorfahIssuePdfRenderer
{
    /// <inheritdoc />
    public Task<byte[]> RenderAsync(string html, CancellationToken cancellationToken) =>
        Task.FromResult(Encoding.UTF8.GetBytes(html));
}
