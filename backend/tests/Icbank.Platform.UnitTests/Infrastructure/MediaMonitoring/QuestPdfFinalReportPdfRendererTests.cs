using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Infrastructure.MediaMonitoring;
using Icbank.Platform.Infrastructure.Rendering;

namespace Icbank.Platform.UnitTests.Infrastructure.MediaMonitoring;

/// <summary>Tests the real QuestPDF-backed <see cref="IFinalReportPdfRenderer"/> implementation.</summary>
public sealed class QuestPdfFinalReportPdfRendererTests
{
    private readonly QuestPdfFinalReportPdfRenderer _renderer = new();

    [Fact]
    public async Task RenderAsync_ArabicHtml_ReturnsBytesStartingWithPdfMagicNumber()
    {
        var html = "<html><body><h1>تقرير الرصد الإعلامي</h1><p>ملخص الفترة</p></body></html>";

        var pdfBytes = await _renderer.RenderAsync(html, CancellationToken.None);

        pdfBytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5).Should().Be("%PDF-");
    }

    [Fact]
    public async Task RenderAsync_OversizedHtml_ThrowsRenderingValidationException()
    {
        var oversizedHtml = new string('س', 30 * 1024 * 1024);

        Func<Task> act = async () => await _renderer.RenderAsync(oversizedHtml, CancellationToken.None);

        await act.Should().ThrowAsync<RenderingValidationException>();
    }
}
