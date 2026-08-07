using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Infrastructure.Rendering;

namespace Icbank.Platform.Infrastructure.Shorfah;

/// <summary>
/// Real <see cref="IShorfahIssuePdfRenderer"/> implementation, replacing the Wave 4a placeholder
/// (<c>TemplateShorfahIssuePdfRenderer</c>, which returned the input HTML bytes unchanged with a
/// spoofed <c>application/pdf</c> content type). Renders the already-HTML-encoded document (see
/// <see cref="ShorfahIssueHtmlBuilder"/>) to a true PDF byte stream via QuestPDF and
/// <see cref="HtmlDocumentPdfComposer"/>, sharing the same embedded-approved-font/RTL pipeline as
/// <see cref="Icbank.Platform.Infrastructure.MediaMonitoring.QuestPdfFinalReportPdfRenderer"/>.
/// </summary>
public sealed class QuestPdfShorfahIssuePdfRenderer : IShorfahIssuePdfRenderer
{
    /// <inheritdoc />
    public async Task<byte[]> RenderAsync(string html, CancellationToken cancellationToken)
    {
        RenderingGuard.EnsureWithinLimit(System.Text.Encoding.UTF8.GetByteCount(html), "Shorfah issue HTML input");
        var pdfBytes = await RenderingGuard.RunWithTimeoutAsync(() => HtmlDocumentPdfComposer.Compose(html), cancellationToken);
        RenderingGuard.EnsureWithinLimit(pdfBytes.LongLength, "Rendered Shorfah issue PDF");
        return pdfBytes;
    }
}
