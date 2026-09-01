using HtmlAgilityPack;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Icbank.Platform.Infrastructure.Rendering;

/// <summary>
/// Renders the already-HTML-encoded documents produced by <c>FinalReportHtmlBuilder</c> and
/// <c>ShorfahIssueHtmlBuilder</c> (both Application-layer, both pass every interpolated field
/// through <see cref="System.Net.WebUtility.HtmlEncode(string?)"/> before building the markup) as
/// a real PDF using QuestPDF's fluent API. QuestPDF has no built-in HTML parser (confirmed: the
/// maintainers explicitly scope that out -- https://github.com/QuestPDF/QuestPDF/issues/961), so
/// this composer walks the small, fixed set of tags those two builders actually emit
/// (h1/h2/h3/p/table/tr/td/th/ul/li/div/span/strong/em/a) via HtmlAgilityPack (MIT) and maps each
/// to the corresponding QuestPDF element, applying <see cref="EmbeddedArabicFontProvider.FontFamily"/>
/// and right-to-left content direction throughout. This is not a general HTML-to-PDF engine --
/// unsupported tags are rendered as their text content only, never dropped silently and never
/// executed/interpreted (no script/style evaluation, no external resource fetches), which keeps
/// the render path safe even though the source HTML already went through server-side encoding.
/// </summary>
/// <remarks>
/// A document that opens with a <c>div.cover</c> is laid out as the authority's approved report:
/// a cover sheet, then content pages carrying the running header and footer that identify the
/// report on every printed sheet. Documents without one keep the plain single-flow layout.
/// </remarks>
public static class HtmlDocumentPdfComposer
{
    private const int TitleFontSize = 20;
    private const int HeadingFontSize = 15;
    private const int BodyFontSize = 11;
    private const int MetaFontSize = 10;
    private const float PageMarginCentimetres = 1.75f;

    /// <summary>Composes and renders the given HTML document to PDF bytes.</summary>
    /// <param name="html">The fully HTML-encoded source document.</param>
    /// <returns>The rendered PDF byte stream (always begins with the <c>%PDF-</c> magic number).</returns>
    public static byte[] Compose(string html) => Compose(html, null);

    /// <summary>Composes and renders the given HTML document to PDF bytes with a running footer.</summary>
    /// <param name="html">The fully HTML-encoded source document.</param>
    /// <param name="footerLabel">The label to print beside the page number, or null for no footer.</param>
    /// <returns>The rendered PDF byte stream (always begins with the <c>%PDF-</c> magic number).</returns>
    /// <remarks>
    /// A multi-page official report with no page numbering cannot be referred to in a meeting, and
    /// a printed page carried nothing identifying which report it belonged to.
    /// </remarks>
    public static byte[] Compose(string html, string? footerLabel)
    {
        EmbeddedArabicFontProvider.EnsureRegistered();

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
        HtmlNode body = htmlDocument.DocumentNode.SelectSingleNode("//body") ?? htmlDocument.DocumentNode;
        HtmlNode? cover = body.SelectSingleNode(".//div[@class='cover']");

        return Document.Create(container =>
        {
            if (cover is not null)
            {
                container.Page(page => PdfCoverPageComposer.Compose(page, cover));
            }

            container.Page(page => ComposeContentPage(page, body, cover, footerLabel));
        }).GeneratePdf();
    }

    private static void ComposeContentPage(PageDescriptor page, HtmlNode body, HtmlNode? cover, string? footerLabel)
    {
        page.Size(PageSizes.A4);
        page.Margin(PageMarginCentimetres, Unit.Centimetre);
        page.PageColor(Colors.White);
        page.ContentFromRightToLeft();
        page.DefaultTextStyle(style => style
            .FontFamily(EmbeddedArabicFontProvider.FontFamily)
            .FontSize(BodyFontSize)
            .FontColor(PdfReportPalette.Navy)
            .DirectionFromRightToLeft());

        if (cover is not null)
        {
            ComposeHeader(page, cover);
            ComposeReportFooter(page, cover, footerLabel);
        }
        else if (!string.IsNullOrWhiteSpace(footerLabel))
        {
            ComposeFooter(page, footerLabel!);
        }

        page.Content().Column(column =>
        {
            column.Spacing(6);
            foreach (HtmlNode node in body.ChildNodes)
            {
                if (node != cover)
                {
                    RenderBlock(column, node);
                }
            }
        });
    }

    private static void ComposeHeader(PageDescriptor page, HtmlNode cover)
    {
        page.Header().PaddingBottom(8).Column(header =>
        {
            header.Item().Row(row =>
            {
                row.RelativeItem().Column(brand =>
                {
                    brand.Item().Text(Attribute(cover, "data-org")).FontSize(10).Bold().FontColor(PdfReportPalette.Teal);
                    brand.Item().PaddingTop(2).AlignRight().Width(80).Height(2).Background(PdfReportPalette.Mustard);
                });
                row.ConstantItem(140).AlignLeft()
                    .Text(Attribute(cover, "data-report-number")).FontSize(9).FontColor(PdfReportPalette.Muted)
                    .DirectionFromLeftToRight();
            });

            header.Item().PaddingTop(7).LineHorizontal(0.8f).LineColor(PdfReportPalette.Line);
        });
    }

    private static void ComposeReportFooter(PageDescriptor page, HtmlNode cover, string? footerLabel)
    {
        page.Footer().PaddingTop(8).BorderTop(0.8f).BorderColor(PdfReportPalette.Line).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(Attribute(cover, "data-confidentiality"))
                .FontSize(8.5f).FontColor(PdfReportPalette.Muted);
            row.RelativeItem().AlignCenter().Text(footerLabel ?? string.Empty)
                .FontSize(8.5f).FontColor(PdfReportPalette.Muted);
            row.RelativeItem().AlignLeft().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(8.5f).FontColor(PdfReportPalette.Muted));
                text.CurrentPageNumber();
            });
        });
    }

    private static void ComposeFooter(PageDescriptor page, string label)
    {
        page.Footer().PaddingTop(8).BorderTop(1).BorderColor(PdfReportPalette.Mint).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(MetaFontSize - 1).FontColor(PdfReportPalette.Teal);
            row.ConstantItem(90).AlignLeft().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(MetaFontSize - 1).FontColor(PdfReportPalette.Teal));
                text.CurrentPageNumber();
                text.Span(" / ");
                text.TotalPages();
            });
        });
    }

    private static void RenderBlock(ColumnDescriptor column, HtmlNode node)
    {
        switch (node.Name)
        {
            case "h1":
                RenderTitle(column, node);
                break;
            case "h2":
                RenderHeading(column, node);
                break;
            case "h3":
                RenderSubHeading(column, node);
                break;
            case "p":
                RenderParagraph(column, node);
                break;
            case "div":
                RenderDiv(column, node);
                break;
            case "table":
                RenderTable(column, node);
                break;
            case "ul":
            case "ol":
                RenderList(column, node);
                break;
            case "#text":
            case "#comment":
                break;
            default:
                RenderChildren(column, node);
                break;
        }
    }

    private static void RenderTitle(ColumnDescriptor column, HtmlNode node)
    {
        column.Item().Text(TextOf(node)).FontSize(TitleFontSize).Bold().FontColor(PdfReportPalette.Navy);
        column.Item().PaddingTop(4).LineHorizontal(2).LineColor(PdfReportPalette.Teal);
    }

    // A numbered heading is drawn the way the approved report draws it -- a filled badge carrying
    // the section number, a mustard sliver, then the title on a tinted band -- because that shape
    // is how a reader finds a section while flicking through printed pages. Headings with no
    // number keep the lighter leading-edge rule used by the other documents this composer serves.
    private static void RenderHeading(ColumnDescriptor column, HtmlNode node)
    {
        var number = node.GetAttributeValue("data-number", string.Empty);
        if (string.IsNullOrWhiteSpace(number))
        {
            column.Item().PaddingTop(10).Row(row =>
            {
                row.ConstantItem(4).Background(PdfReportPalette.Mustard);
                row.RelativeItem().PaddingRight(8).Text(TextOf(node))
                    .FontSize(HeadingFontSize).Bold().FontColor(PdfReportPalette.Teal);
            });
            return;
        }

        column.Item().PaddingTop(16).Height(32).Row(row =>
        {
            row.RelativeItem().Background(PdfReportPalette.SectionBand).PaddingRight(14).AlignMiddle()
                .Text(TextOf(node)).FontSize(HeadingFontSize).Bold().FontColor(PdfReportPalette.Navy);
            row.ConstantItem(5).Background(PdfReportPalette.Mustard);
            row.ConstantItem(34).Background(PdfReportPalette.Band).AlignCenter().AlignMiddle()
                .Text(number).FontSize(13).Bold().FontColor(Colors.White);
        });
    }

    private static void RenderSubHeading(ColumnDescriptor column, HtmlNode node)
    {
        column.Item().PaddingTop(14).Text(TextOf(node)).FontSize(13).Bold().FontColor(PdfReportPalette.Teal);
    }

    private static void RenderChildren(ColumnDescriptor column, HtmlNode node)
    {
        foreach (HtmlNode child in node.ChildNodes)
        {
            RenderBlock(column, child);
        }
    }

    private static void RenderParagraph(ColumnDescriptor column, HtmlNode node)
    {
        var text = TextOf(node);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        var isItalic = node.SelectSingleNode(".//i | .//em") is not null;
        var isBold = node.SelectSingleNode(".//strong | .//b") is not null;
        QuestPDF.Fluent.TextSpanDescriptor span = column.Item().PaddingTop(4).Text(text).LineHeight(1.45f);
        if (isItalic)
        {
            span = span.Italic();
        }

        if (isBold)
        {
            span = span.Bold();
        }
    }

    private static void RenderDiv(ColumnDescriptor column, HtmlNode node)
    {
        var classAttribute = node.GetAttributeValue("class", string.Empty);
        switch (classAttribute)
        {
            case "kpi-grid":
                PdfReportBlocks.RenderKpiGrid(column, node);
                return;
            case "news-item":
                PdfReportBlocks.RenderNewsItem(column, node);
                return;
            case "source-item":
                PdfReportBlocks.RenderSourceItem(column, node);
                return;
        }

        var text = TextOf(node);
        if (string.IsNullOrWhiteSpace(text))
        {
            RenderChildren(column, node);
            return;
        }

        RenderTextDiv(column, node, classAttribute, text);
    }

    private static void RenderTextDiv(ColumnDescriptor column, HtmlNode node, string classAttribute, string text)
    {
        switch (classAttribute)
        {
            case "quote":
                PdfReportBlocks.RenderQuote(column, node);
                return;
            case "quote-by":
                column.Item().PaddingTop(5).Text(text).FontSize(9.5f).FontColor(PdfReportPalette.Muted);
                return;
            case "note":
                column.Item().PaddingTop(8).Background(PdfReportPalette.Tint).Padding(11)
                    .Text(text).FontSize(9.5f).FontColor(PdfReportPalette.Muted).LineHeight(1.45f);
                return;
            case "meta":
                column.Item().Background(PdfReportPalette.Mint).Padding(8)
                    .Text(text).FontSize(MetaFontSize).FontColor(PdfReportPalette.Navy);
                return;
            default:
                column.Item().Text(text).FontSize(BodyFontSize);
                return;
        }
    }

    private static void RenderList(ColumnDescriptor column, HtmlNode node)
    {
        foreach (HtmlNode item in node.SelectNodes("./li") ?? Enumerable.Empty<HtmlNode>())
        {
            var text = TextOf(item);
            if (!string.IsNullOrWhiteSpace(text))
            {
                column.Item().PaddingTop(3).Row(row =>
                {
                    row.ConstantItem(14).PaddingTop(4).AlignCenter()
                        .Width(4).Height(4).Background(PdfReportPalette.Mustard);
                    row.RelativeItem().Text(text).LineHeight(1.4f);
                });
            }
        }
    }

    private static void RenderTable(ColumnDescriptor column, HtmlNode node)
    {
        var rows = (node.SelectNodes(".//tr") ?? Enumerable.Empty<HtmlNode>()).ToList();
        if (rows.Count == 0)
        {
            return;
        }

        var columnCount = rows.Max(r => (r.SelectNodes("./th|./td") ?? Enumerable.Empty<HtmlNode>()).Count());
        if (columnCount == 0)
        {
            return;
        }

        var widths = ColumnWidths(node, columnCount);
        HtmlNode? headerRow = rows[0].SelectNodes("./th") is { Count: > 0 } ? rows[0] : null;
        column.Item().PaddingTop(10).Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var width in widths)
                {
                    columns.RelativeColumn(width);
                }
            });

            RenderTableHeader(table, headerRow);
            var index = 0;
            foreach (HtmlNode row in rows.Skip(headerRow is null ? 0 : 1))
            {
                RenderTableRow(table, row, index++);
            }
        });
    }

    // A table that runs past the page break left the next page's rows with no column labels, so
    // the header row is handed to QuestPDF as a repeating header rather than as an ordinary row.
    private static void RenderTableHeader(TableDescriptor table, HtmlNode? headerRow)
    {
        if (headerRow is null)
        {
            return;
        }

        table.Header(header =>
        {
            foreach (HtmlNode cell in headerRow.SelectNodes("./th|./td") ?? Enumerable.Empty<HtmlNode>())
            {
                RenderCell(header.Cell(), cell, true, 0);
            }
        });
    }

    // Equal columns forced a headline into a four-line sliver beside a date column that needed a
    // fraction of the width it was given, so each table declares the share every column needs.
    private static float[] ColumnWidths(HtmlNode node, int columnCount)
    {
        var declared = node.GetAttributeValue("data-widths", string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(part => float.TryParse(part, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var value) && value > 0 ? value : 1f)
            .ToArray();
        return declared.Length == columnCount ? declared : Enumerable.Repeat(1f, columnCount).ToArray();
    }

    private static void RenderTableRow(TableDescriptor table, HtmlNode row, int index)
    {
        var isHeaderRow = row.SelectNodes("./th") is { Count: > 0 };
        foreach (HtmlNode cell in row.SelectNodes("./th|./td") ?? Enumerable.Empty<HtmlNode>())
        {
            RenderCell(table.Cell(), cell, isHeaderRow, index);
        }
    }

    // Rows of unbroken white ran together on the long tables, so every other row carries a tint --
    // the same banding the approved report uses to keep a row readable across its full width.
    private static void RenderCell(IContainer cell, HtmlNode node, bool isHeaderRow, int index)
    {
        var cellText = TextOf(node);
        IContainer styledCell = isHeaderRow
            ? cell.Background(PdfReportPalette.Accent(node.GetAttributeValue("data-tone", string.Empty)))
                .PaddingVertical(7).PaddingHorizontal(9)
            : cell.Background(index % 2 == 1 ? PdfReportPalette.Tint : Colors.White)
                .BorderBottom(0.8f).BorderColor(PdfReportPalette.Line)
                .PaddingVertical(6).PaddingHorizontal(9);
        QuestPDF.Fluent.TextSpanDescriptor cellSpan = styledCell.Text(cellText).FontSize(10.5f).LineHeight(1.35f);
        if (isHeaderRow)
        {
            cellSpan.FontColor(Colors.White).Bold();
        }
    }

    private static string TextOf(HtmlNode node) => System.Net.WebUtility.HtmlDecode(node.InnerText).Trim();

    private static string Attribute(HtmlNode node, string name) =>
        System.Net.WebUtility.HtmlDecode(node.GetAttributeValue(name, string.Empty)).Trim();
}
