using HtmlAgilityPack;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Icbank.Platform.Infrastructure.Rendering;

/// <summary>
/// Draws the cover sheet the authority's approved media-monitoring report opens with: a full-bleed
/// dark band carrying the authority's name in both languages, a mustard rule under it, the report
/// title block, and the identification table a reader needs before circulating the document
/// (period, preparing department, recipient, reference number, issue date). A report that opened
/// straight onto its first table could not be told apart from a printout of the screen.
/// </summary>
internal static class PdfCoverPageComposer
{
    /// <summary>Composes the cover page from the document's cover node.</summary>
    /// <param name="page">The page to draw on.</param>
    /// <param name="cover">The <c>div.cover</c> node holding the cover's text and attributes.</param>
    internal static void Compose(PageDescriptor page, HtmlNode cover)
    {
        page.Size(PageSizes.A4);
        page.Margin(0);
        page.PageColor(Colors.White);
        page.ContentFromRightToLeft();
        page.DefaultTextStyle(style => style
            .FontFamily(EmbeddedArabicFontProvider.FontFamily)
            .DirectionFromRightToLeft());

        page.Content().Column(column =>
        {
            ComposeBand(column, cover);
            column.Item().PaddingHorizontal(52).PaddingTop(78).Column(inner => ComposeTitleBlock(inner, cover));
        });

        page.Footer().PaddingBottom(28).AlignCenter()
            .Text(Attribute(cover, "data-confidentiality")).FontSize(9).FontColor(PdfReportPalette.Muted);
    }

    private static void ComposeBand(ColumnDescriptor column, HtmlNode cover)
    {
        column.Item().Background(PdfReportPalette.Band).PaddingVertical(28).PaddingHorizontal(52).Column(band =>
        {
            band.Item().Text(Attribute(cover, "data-org")).FontSize(17).Bold().FontColor(Colors.White);
            band.Item().PaddingTop(3).Text(Attribute(cover, "data-org-en")).FontSize(10)
                .FontColor(PdfReportPalette.BandSubtitle).DirectionFromLeftToRight();
        });

        column.Item().Height(5).Background(PdfReportPalette.Mustard);
    }

    private static void ComposeTitleBlock(ColumnDescriptor column, HtmlNode cover)
    {
        column.Item().Text(Attribute(cover, "data-kicker")).FontSize(10.5f).Bold().FontColor(PdfReportPalette.Mustard);
        column.Item().PaddingTop(16).Text(NodeText(cover, ".//h1")).FontSize(29).Bold().FontColor(PdfReportPalette.Navy);
        column.Item().PaddingTop(10).Text(NodeText(cover, ".//div[@class='cover-subtitle']"))
            .FontSize(13.5f).FontColor(PdfReportPalette.Teal);
        column.Item().PaddingTop(16).Width(72).Height(3).Background(PdfReportPalette.Mustard);
        column.Item().PaddingTop(40).Element(container => ComposeMetaTable(container, cover));
    }

    private static void ComposeMetaTable(IContainer container, HtmlNode cover)
    {
        var rows = (cover.SelectNodes(".//table[@class='cover-meta']//tr") ?? Enumerable.Empty<HtmlNode>()).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        container.Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                columns.RelativeColumn(1.5f);
                columns.RelativeColumn(3.5f);
            });

            foreach (HtmlNode row in rows)
            {
                var cells = (row.SelectNodes("./td") ?? Enumerable.Empty<HtmlNode>()).ToList();
                if (cells.Count == 2)
                {
                    ComposeMetaRow(table, cells[0], cells[1]);
                }
            }
        });
    }

    private static void ComposeMetaRow(TableDescriptor table, HtmlNode label, HtmlNode value)
    {
        table.Cell().Background(PdfReportPalette.SectionBand).Border(0.8f).BorderColor(PdfReportPalette.Line)
            .PaddingVertical(9).PaddingHorizontal(12)
            .Text(Text(label)).FontSize(10.5f).Bold().FontColor(PdfReportPalette.Navy);
        table.Cell().Border(0.8f).BorderColor(PdfReportPalette.Line)
            .PaddingVertical(9).PaddingHorizontal(12)
            .Text(Text(value)).FontSize(10.5f).FontColor(PdfReportPalette.Navy);
    }

    private static string NodeText(HtmlNode cover, string xpath)
    {
        HtmlNode? node = cover.SelectSingleNode(xpath);
        return node is null ? string.Empty : Text(node);
    }

    private static string Text(HtmlNode node) => System.Net.WebUtility.HtmlDecode(node.InnerText).Trim();

    private static string Attribute(HtmlNode node, string name) =>
        System.Net.WebUtility.HtmlDecode(node.GetAttributeValue(name, string.Empty)).Trim();
}
