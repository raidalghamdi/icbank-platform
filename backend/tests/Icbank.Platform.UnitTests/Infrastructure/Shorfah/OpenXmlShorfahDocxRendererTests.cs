using System.IO.Compression;
using DocumentFormat.OpenXml.Packaging;
using FluentAssertions;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Infrastructure.Rendering;
using Icbank.Platform.Infrastructure.Shorfah;

namespace Icbank.Platform.UnitTests.Infrastructure.Shorfah;

/// <summary>Tests the real OpenXml-backed <see cref="IShorfahDocxRenderer"/> implementation.</summary>
public sealed class OpenXmlShorfahDocxRendererTests
{
    private const string TitleAr = "نشرة شرفة الأسبوعية";
    private const string BodyAr = "هذا هو محتوى النشرة.\n\nفقرة ثانية بالعربية.";

    private readonly OpenXmlShorfahDocxRenderer _renderer = new();

    [Fact]
    public async Task RenderAsync_ProducesValidOpcZipContainingWordDocumentXml()
    {
        var docxBytes = await _renderer.RenderAsync(TitleAr, BodyAr, CancellationToken.None);

        docxBytes.Should().NotBeEmpty();

        using var stream = new MemoryStream(docxBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.GetEntry("word/document.xml").Should().NotBeNull("a valid .docx is an OPC zip package with a word/document.xml part");
    }

    [Fact]
    public async Task RenderAsync_ArabicTitleAndBody_SurviveRoundTripViaOpenXml()
    {
        var docxBytes = await _renderer.RenderAsync(TitleAr, BodyAr, CancellationToken.None);

        using var stream = new MemoryStream(docxBytes);
        using var package = WordprocessingDocument.Open(stream, isEditable: false);
        var allText = package.MainDocumentPart!.Document!.Body!.InnerText;

        allText.Should().Contain(TitleAr);
        allText.Should().Contain("هذا هو محتوى النشرة");
        allText.Should().Contain("فقرة ثانية بالعربية");
    }

    [Fact]
    public async Task RenderAsync_TitleParagraph_HasRtlParagraphProperties()
    {
        var docxBytes = await _renderer.RenderAsync(TitleAr, BodyAr, CancellationToken.None);

        using var stream = new MemoryStream(docxBytes);
        using var package = WordprocessingDocument.Open(stream, isEditable: false);
        DocumentFormat.OpenXml.Wordprocessing.Paragraph firstParagraph =
            package.MainDocumentPart!.Document!.Body!.Elements<DocumentFormat.OpenXml.Wordprocessing.Paragraph>().First();

        firstParagraph.ParagraphProperties!.BiDi.Should().NotBeNull("every paragraph must carry explicit RTL markup, not rely on client autodetection");
    }

    [Fact]
    public async Task RenderAsync_OversizedInput_ThrowsRenderingValidationException()
    {
        var oversizedBody = new string('س', 30 * 1024 * 1024);

        Func<Task> act = async () => await _renderer.RenderAsync(TitleAr, oversizedBody, CancellationToken.None);

        await act.Should().ThrowAsync<RenderingValidationException>();
    }
}
