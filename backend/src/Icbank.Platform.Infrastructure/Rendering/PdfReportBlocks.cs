using HtmlAgilityPack;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Icbank.Platform.Infrastructure.Rendering;

/// <summary>
/// The non-tabular fragments of the approved report layout: the indicator card grid, the news
/// entries, the pulled quote and the source list. They live apart from the generic HTML walker
/// because each one is a fixed piece of the authority's report design rather than a mapping of an
/// HTML tag -- a reader recognises the report by these shapes, and a table of numbers where the
/// card grid belongs reads as a different document altogether.
/// </summary>
internal static class PdfReportBlocks
{
    private const int CardsPerRow = 3;
    private const int LongValueLength = 8;
    private const int CardMinHeight = 84;

    /// <summary>Renders the indicator cards as a three-per-row grid.</summary>
    /// <param name="column">The column to append to.</param>
    /// <param name="grid">The <c>div.kpi-grid</c> node holding one child div per card.</param>
    internal static void RenderKpiGrid(ColumnDescriptor column, HtmlNode grid)
    {
        var cards = (grid.SelectNodes("./div") ?? Enumerable.Empty<HtmlNode>()).ToList();
        if (cards.Count == 0)
        {
            return;
        }

        column.Item().PaddingTop(12).Column(rows =>
        {
            rows.Spacing(9);
            for (var start = 0; start < cards.Count; start += CardsPerRow)
            {
                var chunk = cards.Skip(start).Take(CardsPerRow).ToList();
                rows.Item().ShowEntire().Row(row =>
                {
                    row.Spacing(9);
                    foreach (HtmlNode card in chunk)
                    {
                        row.RelativeItem().Element(container => RenderKpiCard(container, card));
                    }

                    for (var filler = chunk.Count; filler < CardsPerRow; filler++)
                    {
                        row.RelativeItem();
                    }
                });
            }
        });
    }

    /// <summary>Renders one news entry: numbered badge, headline, meta line, body and source.</summary>
    /// <param name="column">The column to append to.</param>
    /// <param name="node">The <c>div.news-item</c> node.</param>
    internal static void RenderNewsItem(ColumnDescriptor column, HtmlNode node)
    {
        column.Item().PaddingTop(13).Column(item =>
        {
            item.Item().Row(row =>
            {
                row.ConstantItem(21).Height(21).Background(PdfReportPalette.Teal).AlignCenter().AlignMiddle()
                    .Text(Attribute(node, "data-index")).FontSize(10).Bold().FontColor(Colors.White);
                row.RelativeItem().PaddingRight(9).AlignMiddle()
                    .Text(Child(node, "news-headline")).FontSize(12.5f).Bold().FontColor(PdfReportPalette.Teal);
            });

            item.Item().PaddingTop(4).PaddingRight(30)
                .Text(Child(node, "news-meta")).FontSize(9).FontColor(PdfReportPalette.Muted);
            RenderNewsBody(item, node);
            item.Item().PaddingTop(5).PaddingRight(30)
                .Text(Child(node, "news-source")).FontSize(9).Bold().FontColor(PdfReportPalette.Mustard);
        });
    }

    /// <summary>Renders the pulled quote inside its tinted box.</summary>
    /// <param name="column">The column to append to.</param>
    /// <param name="node">The <c>div.quote</c> node.</param>
    internal static void RenderQuote(ColumnDescriptor column, HtmlNode node)
    {
        column.Item().PaddingTop(10).Background(PdfReportPalette.Tint)
            .BorderRight(4).BorderColor(PdfReportPalette.Mustard).Padding(13)
            .Text(Text(node)).FontSize(11.5f).Italic().FontColor(PdfReportPalette.Navy).LineHeight(1.5f);
    }

    /// <summary>Renders one numbered source with its link on a line of its own.</summary>
    /// <param name="column">The column to append to.</param>
    /// <param name="node">The <c>div.source-item</c> node.</param>
    internal static void RenderSourceItem(ColumnDescriptor column, HtmlNode node)
    {
        column.Item().PaddingTop(7).Column(item =>
        {
            item.Item().Text(Child(node, "source-name")).FontSize(10.5f).FontColor(PdfReportPalette.Navy);
            var url = Child(node, "source-url");
            if (!string.IsNullOrWhiteSpace(url))
            {
                item.Item().PaddingTop(1).AlignLeft()
                    .Text(url).FontSize(8.5f).FontColor(PdfReportPalette.Teal).DirectionFromLeftToRight();
            }
        });
    }

    private static void RenderNewsBody(ColumnDescriptor item, HtmlNode node)
    {
        foreach (HtmlNode paragraph in node.SelectNodes("./p") ?? Enumerable.Empty<HtmlNode>())
        {
            var text = Text(paragraph);
            if (!string.IsNullOrWhiteSpace(text))
            {
                item.Item().PaddingTop(5).PaddingRight(30).Text(text).FontSize(11).LineHeight(1.45f);
            }
        }
    }

    private static void RenderKpiCard(IContainer container, HtmlNode card)
    {
        var accent = PdfReportPalette.Accent(Attribute(card, "data-accent"));
        var value = Child(card, "kpi-value");

        // A written-out indicator ("واسع النطاق عبر المنصات") set at the figure size grew taller than
        // the card it sits in and pushed the whole row onto the next page, so wordy values step down.
        var valueSize = value.Length > LongValueLength ? 12f : 21f;
        container.Background(accent).PaddingTop(3).Background(PdfReportPalette.Tint)
            .Border(0.8f).BorderColor(PdfReportPalette.Line).MinHeight(CardMinHeight)
            .PaddingVertical(12).PaddingHorizontal(8)
            .Column(inner =>
            {
                inner.Item().AlignCenter().Text(value).FontSize(valueSize).Bold().FontColor(accent);
                inner.Item().PaddingTop(4).AlignCenter()
                    .Text(Child(card, "kpi-label")).FontSize(10.5f).Bold().FontColor(PdfReportPalette.Navy);
                inner.Item().PaddingTop(2).AlignCenter()
                    .Text(Child(card, "kpi-sub")).FontSize(8).FontColor(PdfReportPalette.Muted);
            });
    }

    private static string Child(HtmlNode node, string className)
    {
        HtmlNode? child = node.SelectSingleNode($".//*[@class='{className}']");
        return child is null ? string.Empty : Text(child);
    }

    private static string Text(HtmlNode node) => System.Net.WebUtility.HtmlDecode(node.InnerText).Trim();

    private static string Attribute(HtmlNode node, string name) =>
        System.Net.WebUtility.HtmlDecode(node.GetAttributeValue(name, string.Empty)).Trim();
}
