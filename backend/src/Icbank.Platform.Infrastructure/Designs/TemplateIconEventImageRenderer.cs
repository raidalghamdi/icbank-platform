using System.Text;
using Icbank.Platform.Application.Designs.IconEvent;
using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>
/// Deterministic, non-Chromium default <see cref="IIconEventImageRenderer"/> implementation.
/// Matches the <c>TemplateFinalReportPdfRenderer</c> precedent exactly: returns the UTF-8 bytes
/// of the already-encoded HTML document unchanged, so callers must not assume a PNG magic number
/// until a real headless-rendering engine is wired (see WAVE3B-PORT-NOTES.md).
/// </summary>
public sealed class TemplateIconEventImageRenderer : IIconEventImageRenderer
{
    /// <inheritdoc />
    public Task<byte[]> RenderAsync(string html, IconEventSizePreset size, bool isUltraQuality, CancellationToken cancellationToken) =>
        Task.FromResult(Encoding.UTF8.GetBytes(html));
}
