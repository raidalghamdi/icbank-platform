using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Icbank.Platform.Infrastructure.Rendering;

/// <summary>
/// Shared helpers for building right-to-left Arabic <c>.docx</c> content via
/// <c>DocumentFormat.OpenXml</c>, used by both <see cref="Icbank.Platform.Infrastructure.Shorfah.OpenXmlShorfahDocxRenderer"/>
/// and <see cref="Icbank.Platform.Infrastructure.AiYear.OpenXmlAiYearReportDocxBuilder"/>. Every
/// paragraph gets explicit <see cref="ParagraphMarkRunProperties"/>/<see cref="Justification"/> RTL
/// markup (<c>&lt;w:bidi/&gt;</c>, right justification) rather than relying on Word's
/// autodetection, since autodetection is a client-side heuristic and this document must render
/// correctly regardless of the opening application's language settings.
/// </summary>
public static class OpenXmlRtlHelpers
{
    /// <summary>Builds an RTL-flagged, right-justified <see cref="ParagraphProperties"/> instance, optionally with a heading style.</summary>
    /// <param name="styleId">An optional Word heading style id (e.g. <c>"Heading1"</c>).</param>
    /// <returns>The paragraph properties to attach to a new <see cref="Paragraph"/>.</returns>
    public static ParagraphProperties BuildRtlParagraphProperties(string? styleId = null)
    {
        var properties = new ParagraphProperties
        {
            Justification = new Justification { Val = JustificationValues.Right },
            BiDi = new BiDi(),
        };

        if (styleId is not null)
        {
            properties.ParagraphStyleId = new ParagraphStyleId { Val = styleId };
        }

        return properties;
    }

    /// <summary>Builds an RTL-flagged <see cref="RunProperties"/> instance (sets right-to-left run direction and, optionally, bold).</summary>
    /// <param name="bold">Whether the run should be bold.</param>
    /// <param name="italic">Whether the run should be italic.</param>
    /// <returns>The run properties to attach to a new <see cref="Run"/>.</returns>
    public static RunProperties BuildRtlRunProperties(bool bold = false, bool italic = false)
    {
        var properties = new RunProperties { RightToLeftText = new RightToLeftText() };
        if (bold)
        {
            properties.Bold = new Bold();
        }

        if (italic)
        {
            properties.Italic = new Italic();
        }

        return properties;
    }

    /// <summary>Builds a plain RTL paragraph containing a single text run, preserving spaces via <see cref="SpaceProcessingModeValues.Preserve"/>.</summary>
    /// <param name="text">The paragraph's text content. Rendered literally -- OpenXml text runs are never interpreted as markup, so no HTML-style encoding is required or applied here.</param>
    /// <param name="bold">Whether the run should be bold.</param>
    /// <param name="styleId">An optional Word heading style id.</param>
    /// <returns>The assembled paragraph.</returns>
    public static Paragraph BuildParagraph(string text, bool bold = false, string? styleId = null)
    {
        var run = new Run(BuildRtlRunProperties(bold), new Text(text) { Space = SpaceProcessingModeValues.Preserve });
        return new Paragraph(BuildRtlParagraphProperties(styleId), run);
    }

    /// <summary>Builds a single-cell-wide table cell containing one RTL paragraph, for use in a <see cref="TableRow"/>.</summary>
    /// <param name="text">The cell's text content.</param>
    /// <param name="bold">Whether the cell's run should be bold (used for header rows).</param>
    /// <returns>The assembled table cell.</returns>
    public static TableCell BuildTableCell(string text, bool bold = false)
    {
        var cell = new TableCell();
        cell.Append(new TableCellProperties(new TableCellWidth { Type = TableWidthUnitValues.Auto }));
        cell.Append(BuildParagraph(text, bold));
        return cell;
    }
}
