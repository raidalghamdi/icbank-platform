using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.IconEvent;

/// <summary>
/// Port for rasterizing an HTML poster document to a PNG image (ports
/// <c>POST /designs/icon-event/render</c>'s headless-Chromium pipeline, BUSINESS-RULES.md §7.5).
/// Standing up a headless-browser dependency is out of scope for this wave, matching the
/// <c>TemplateFinalReportPdfRenderer</c> precedent (WAVE3A-PORT-NOTES.md §4.1); the default
/// implementation returns the UTF-8 bytes of the HTML document with a PNG content-type contract
/// preserved so every downstream concern (authorization, rate limiting, audit logging, storage
/// persistence, 404/size validation) is fully exercised end-to-end.
/// </summary>
public interface IIconEventImageRenderer
{
    /// <summary>Renders the given HTML to image bytes at the given size/quality.</summary>
    /// <param name="html">The HTML document to rasterize.</param>
    /// <param name="size">The target size preset.</param>
    /// <param name="isUltraQuality">Whether to use 4x (ultra) instead of 3x (HD) device scale factor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The rendered image bytes.</returns>
    Task<byte[]> RenderAsync(string html, IconEventSizePreset size, bool isUltraQuality, CancellationToken cancellationToken);
}
