using System.Text;
using Icbank.Platform.Application.Designs.Composer;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>
/// Deterministic, non-sharp/Pango default <see cref="IDesignComposer"/> implementation. Emits a
/// small, clearly-labeled UTF-8 text payload describing the compose request instead of a real
/// raster image -- matching the same "never a fabricated success, never invented binary data"
/// contract as <c>TemplateFinalReportPdfRenderer</c>'s counterpart in Wave 3a. Callers must
/// not assume a PNG magic number until a real sharp/Pango (or ImageSharp/SkiaSharp) pipeline is
/// wired, see WAVE3B-PORT-NOTES.md.
/// </summary>
public sealed class PlaceholderDesignComposer : IDesignComposer
{
    /// <inheritdoc />
    public Task<byte[]> ComposeAsync(ComposeDesignInput input, CancellationToken cancellationToken)
    {
        var description = $"COMPOSED-PLACEHOLDER template={input.Template.Id} title={input.TitleText} body={input.BodyText} logos={input.SelectedLogos.Count}";
        return Task.FromResult(Encoding.UTF8.GetBytes(description));
    }
}
