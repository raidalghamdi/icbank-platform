using FluentAssertions;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Infrastructure.Shorfah;

namespace Icbank.Platform.UnitTests.Infrastructure.Shorfah;

/// <summary>Tests the real QuestPDF-backed <see cref="IShorfahIssuePdfRenderer"/> implementation.</summary>
public sealed class QuestPdfShorfahIssuePdfRendererTests
{
    private readonly QuestPdfShorfahIssuePdfRenderer _renderer = new();

    [Fact]
    public async Task RenderAsync_ArabicHtml_ReturnsBytesStartingWithPdfMagicNumber()
    {
        var html = "<html><body><h1>نشرة شرفة</h1><p>محتوى النشرة باللغة العربية</p></body></html>";

        var pdfBytes = await _renderer.RenderAsync(html, CancellationToken.None);

        pdfBytes.Should().NotBeEmpty();
        System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 5).Should().Be("%PDF-");
    }
}
