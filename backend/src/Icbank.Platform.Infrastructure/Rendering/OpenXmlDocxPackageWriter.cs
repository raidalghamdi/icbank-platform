using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace Icbank.Platform.Infrastructure.Rendering;

/// <summary>
/// Writes a minimal, valid <c>.docx</c> OOXML package (a real ZIP/OPC container with a
/// <c>word/document.xml</c> part) from a list of already-built body elements
/// (<see cref="Paragraph"/>, <see cref="Table"/>). Shared by both
/// <see cref="Icbank.Platform.Infrastructure.Shorfah.OpenXmlShorfahDocxRenderer"/> and
/// <see cref="Icbank.Platform.Infrastructure.AiYear.OpenXmlAiYearReportDocxBuilder"/> so the
/// package-assembly plumbing (main document part, sectPr, RTL document-level default) is not
/// duplicated. Sets the document's default <c>&lt;w:bidi/&gt;</c> at the section level so Word
/// opens the whole document in RTL reading order even if a client ignores per-paragraph bidi.
/// </summary>
public static class OpenXmlDocxPackageWriter
{
    /// <summary>Builds a complete <c>.docx</c> byte stream from the given body elements.</summary>
    /// <param name="bodyElements">The paragraphs/tables to place in the document body, in order.</param>
    /// <returns>The rendered <c>.docx</c> bytes (a valid OPC ZIP package with <c>word/document.xml</c>).</returns>
    public static byte[] Build(IReadOnlyList<OpenXmlCompositeElement> bodyElements)
    {
        using var stream = new MemoryStream();
        using (var package = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            MainDocumentPart mainPart = package.AddMainDocumentPart();
            mainPart.Document = new Document();
            Body body = mainPart.Document.AppendChild(new Body());

            foreach (OpenXmlCompositeElement element in bodyElements)
            {
                body.AppendChild(element);
            }

            body.AppendChild(new SectionProperties(new BiDi()));
            mainPart.Document.Save();
        }

        return stream.ToArray();
    }
}
