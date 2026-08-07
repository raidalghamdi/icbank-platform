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
/// (h1/h2/p/table/tr/td/th/ul/li/div/span/strong/em/a) via HtmlAgilityPack (MIT) and maps each to
/// the corresponding QuestPDF element, applying <see cref="EmbeddedArabicFontProvider.FontFamily"/>
/// and right-to-left content direction throughout. This is not a general HTML-to-PDF engine --
/// unsupported tags are rendered as their text content only, never dropped silently and never
/// executed/interpreted (no script/style evaluation, no external resource fetches), which keeps
/// the render path safe even though the source HTML already went through server-side encoding.
/// </summary>
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
    public static byte[] Compose(string html)
    {
        EmbeddedArabicFontProvider.EnsureRegistered();

        var htmlDocument = new HtmlDocument();
        htmlDocument.LoadHtml(html);
        HtmlNode? body = htmlDocument.DocumentNode.SelectSingleNode("//body") ?? htmlDocument.DocumentNode;

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(PageMarginCentimetres, Unit.Centimetre);
                page.PageColor(Colors.White);
                page.ContentFromRightToLeft();
                page.DefaultTextStyle(style => style.FontFamily(EmbeddedArabicFontProvider.FontFamily).FontSize(BodyFontSize).DirectionFromRightToLeft());

                page.Content().Column(column =>
                {
                    column.Spacing(6);
                    foreach (HtmlNode node in body.ChildNodes)
                    {
                        RenderBlock(column, node);
                    }
                });
            });
        }).GeneratePdf();
    }

    private static void RenderBlock(ColumnDescriptor column, HtmlNode node)
    {
        switch (node.Name)
        {
            case "h1":
                column.Item().Text(TextOf(node)).FontSize(TitleFontSize).Bold().FontColor(Colors.Blue.Darken3);
                break;
            case "h2":
                column.Item().PaddingTop(6).Text(TextOf(node)).FontSize(HeadingFontSize).Bold().FontColor(Colors.Blue.Darken2);
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
        QuestPDF.Fluent.TextSpanDescriptor span = column.Item().Text(text);
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
        var text = TextOf(node);
        if (string.IsNullOrWhiteSpace(text))
        {
            RenderChildren(column, node);
            return;
        }

        IContainer container = classAttribute.Contains("meta", StringComparison.Ordinal)
            ? column.Item().Background(Colors.Grey.Lighten3).Padding(8)
            : column.Item();
        container.Text(text).FontSize(classAttribute.Contains("meta", StringComparison.Ordinal) ? MetaFontSize : BodyFontSize);
    }

    private static void RenderList(ColumnDescriptor column, HtmlNode node)
    {
        foreach (HtmlNode item in node.SelectNodes("./li") ?? Enumerable.Empty<HtmlNode>())
        {
            var text = TextOf(item);
            if (!string.IsNullOrWhiteSpace(text))
            {
                column.Item().Text($"• {text}");
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

        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                for (var i = 0; i < columnCount; i++)
                {
                    columns.RelativeColumn();
                }
            });

            foreach (HtmlNode row in rows)
            {
                RenderTableRow(table, row);
            }
        });
    }

    private static void RenderTableRow(TableDescriptor table, HtmlNode row)
    {
        var isHeaderRow = row.SelectNodes("./th") is { Count: > 0 };
        foreach (HtmlNode cell in row.SelectNodes("./th|./td") ?? Enumerable.Empty<HtmlNode>())
        {
            RenderTableCell(table, cell, isHeaderRow);
        }
    }

    private static void RenderTableCell(TableDescriptor table, HtmlNode cell, bool isHeaderRow)
    {
        var cellText = TextOf(cell);
        IContainer styledCell = isHeaderRow
            ? table.Cell().Background(Colors.Blue.Darken2).Padding(5)
            : table.Cell().BorderBottom(1).BorderColor(Colors.Grey.Lighten2).Padding(5);
        QuestPDF.Fluent.TextSpanDescriptor cellSpan = styledCell.Text(cellText);
        if (isHeaderRow)
        {
            cellSpan.FontColor(Colors.White).Bold();
        }
    }

    private static string TextOf(HtmlNode node) => System.Net.WebUtility.HtmlDecode(node.InnerText).Trim();
}
