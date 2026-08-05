using Icbank.Platform.Application.AiYear.Queries;

namespace Icbank.Platform.Application.AiYear;

/// <summary>
/// Port for rendering the assembled <see cref="AiYearReportDataDto"/> into the real <c>.docx</c>
/// byte stream the Node original produced (<c>ai-year.ts:440-569</c>, using the <c>docx</c> npm
/// package's <c>Document</c>/<c>Paragraph</c>/<c>Table</c> tree). Introduced to close the Wave 2
/// regression flagged in WAVE2-PORT-NOTES.md item 16: <c>POST /api/v1/ai-year/report</c> had been
/// returning the JSON data payload instead of a <c>.docx</c> file.
/// </summary>
public interface IAiYearReportDocxRenderer
{
    /// <summary>Renders the report data into a <c>.docx</c> byte stream.</summary>
    /// <param name="report">The assembled report data.</param>
    /// <param name="cancellationToken">A token used to observe cancellation requests.</param>
    /// <returns>The rendered <c>.docx</c> bytes.</returns>
    Task<byte[]> RenderAsync(AiYearReportDataDto report, CancellationToken cancellationToken);
}
