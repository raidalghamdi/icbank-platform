using System.Text;
using Icbank.Platform.Application.MediaMonitoring;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Deterministic, non-AI/non-Chromium default <see cref="IFinalReportPdfRenderer"/>
/// implementation. The Node source used headless Chromium/Puppeteer to rasterize
/// <see cref="FinalReportHtmlBuilder"/>'s HTML output into a true PDF byte stream; wiring a real
/// headless-browser/PDF engine is deferred for Wave 3a (see WAVE3A-PORT-NOTES.md) -- this
/// implementation returns the UTF-8 bytes of the already-encoded HTML document unchanged, so the
/// export-pdf endpoint is fully exercisable end-to-end (persistence, authorization, audit log)
/// without an external rendering dependency. The returned bytes are intentionally HTML, not a
/// binary PDF; callers must not assume a <c>%PDF-</c> magic number until a real engine is wired.
/// </summary>
public sealed class TemplateFinalReportPdfRenderer : IFinalReportPdfRenderer
{
    /// <inheritdoc />
    public Task<byte[]> RenderAsync(string html, CancellationToken cancellationToken) =>
        Task.FromResult(Encoding.UTF8.GetBytes(html));
}
