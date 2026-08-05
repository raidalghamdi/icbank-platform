using System.IO.Compression;
using DocumentFormat.OpenXml.Packaging;
using FluentAssertions;
using Icbank.Platform.Application.AiYear;
using Icbank.Platform.Application.AiYear.Queries;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Infrastructure.AiYear;
using NSubstitute;

namespace Icbank.Platform.UnitTests.Infrastructure.AiYear;

/// <summary>
/// Tests the real <see cref="IAiYearReportDocxRenderer"/> implementation that reproduces the
/// Node <c>ai-year.ts:440-569</c> document tree, closing the WAVE2-PORT-NOTES.md item 16
/// regression.
/// </summary>
public sealed class OpenXmlAiYearReportDocxBuilderTests
{
    private static readonly string[] Row1Channels = { "تويتر", "الموقع" };
    private static readonly string[] Row2Channels = { "انستغرام" };
    private static readonly AiYearReportRowDto[] NoTopByReach = Array.Empty<AiYearReportRowDto>();

    private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();
    private readonly OpenXmlAiYearReportDocxBuilder _builder;

    public OpenXmlAiYearReportDocxBuilderTests()
    {
        _dateTimeProvider.RiyadhNow.Returns(new DateTimeOffset(2026, 8, 5, 22, 31, 0, TimeSpan.FromHours(3)));
        _builder = new OpenXmlAiYearReportDocxBuilder(_dateTimeProvider);
    }

    [Fact]
    public async Task RenderAsync_ProducesValidOpcZipContainingWordDocumentXml()
    {
        var docxBytes = await _builder.RenderAsync(BuildReport(includeTop3: true), CancellationToken.None);

        using var stream = new MemoryStream(docxBytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);
        archive.GetEntry("word/document.xml").Should().NotBeNull();
    }

    [Fact]
    public async Task RenderAsync_WithTopByReach_IncludesTop3HeadingAndTableRows()
    {
        var docxBytes = await _builder.RenderAsync(BuildReport(includeTop3: true), CancellationToken.None);

        using var stream = new MemoryStream(docxBytes);
        using var package = WordprocessingDocument.Open(stream, isEditable: false);
        var text = package.MainDocumentPart!.Document!.Body!.InnerText;

        text.Should().Contain("تقرير عام الذكاء الاصطناعي ٢٠٢٦");
        text.Should().Contain("أبرز ٣ تفعيلات (بحسب الوصول)", "TopByReach is non-empty so the conditional section must render, matching the Node array-spread condition");
        text.Should().Contain("تفعيلة الذكاء الاصطناعي الأولى");
        text.Should().Contain("يناير");
        text.Should().Contain("إجمالي التفعيلات: 2");
    }

    [Fact]
    public async Task RenderAsync_WithoutTopByReach_OmitsTop3Heading()
    {
        var docxBytes = await _builder.RenderAsync(BuildReport(includeTop3: false), CancellationToken.None);

        using var stream = new MemoryStream(docxBytes);
        using var package = WordprocessingDocument.Open(stream, isEditable: false);
        var text = package.MainDocumentPart!.Document!.Body!.InnerText;

        text.Should().NotContain("أبرز ٣ تفعيلات", "the Node source only spreads the top-3 section into the tree when the array is non-empty");
    }

    [Fact]
    public async Task RenderAsync_UsesRiyadhNowNotSystemClock()
    {
        var docxBytes = await _builder.RenderAsync(BuildReport(includeTop3: false), CancellationToken.None);

        using var stream = new MemoryStream(docxBytes);
        using var package = WordprocessingDocument.Open(stream, isEditable: false);
        var text = package.MainDocumentPart!.Document!.Body!.InnerText;

        text.Should().Contain("أغسطس", "the fake IDateTimeProvider.RiyadhNow returns August 2026, and the builder must consume it rather than DateTime.Now");
        _ = _dateTimeProvider.Received(1).RiyadhNow;
    }

    private static AiYearReportDataDto BuildReport(bool includeTop3)
    {
        var row1 = new AiYearReportRowDto(
            Title: "تفعيلة الذكاء الاصطناعي الأولى",
            Month: 1,
            MonthNameAr: "يناير",
            Type: "ورشة عمل",
            Channels: Row1Channels,
            Reach: 1500);
        var row2 = new AiYearReportRowDto(
            Title: "تفعيلة الذكاء الاصطناعي الثانية",
            Month: 2,
            MonthNameAr: "فبراير",
            Type: "ندوة",
            Channels: Row2Channels,
            Reach: null);
        AiYearReportRowDto[] rows = { row1, row2 };
        AiYearReportRowDto[] top3 = { row1 };

        return new AiYearReportDataDto(
            TotalActivations: 2,
            TotalMedia: 5,
            TotalChannels: 3,
            ByType: new Dictionary<string, int> { ["ورشة عمل"] = 1, ["ندوة"] = 1 },
            TopByReach: includeTop3 ? top3 : NoTopByReach,
            Rows: rows);
    }
}
