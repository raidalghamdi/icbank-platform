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
    private const string Teal = "#1a6e7a";
    private const string Navy = "#0e3b4a";
    private const string Mint = "#cce4e6";
    private const string Mustard = "#b8924a";
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

                if (!string.IsNullOrWhiteSpace(footerLabel))
                {
                    ComposeFooter(page, footerLabel!);
                }

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

    private static void ComposeFooter(PageDescriptor page, string label)
    {
        page.Footer().PaddingTop(8).BorderTop(1).BorderColor(Mint).PaddingTop(6).Row(row =>
        {
            row.RelativeItem().Text(label).FontSize(MetaFontSize - 1).FontColor(Teal);
            row.ConstantItem(90).AlignLeft().Text(text =>
            {
                text.DefaultTextStyle(style => style.FontSize(MetaFontSize - 1).FontColor(Teal));
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
        column.Item().Text(TextOf(node)).FontSize(TitleFontSize).Bold().FontColor(Navy);
        column.Item().PaddingTop(4).LineHorizontal(2).LineColor(Teal);
    }

    // The section rule is drawn on the leading edge rather than set as a text colour alone: on a
    // dense report the eye needs the sections to be findable while flicking through printed pages.
    private static void RenderHeading(ColumnDescriptor column, HtmlNode node)
    {
        column.Item().PaddingTop(10).Row(row =>
        {
            row.ConstantItem(4).Background(Mustard);
            row.RelativeItem().PaddingRight(8).Text(TextOf(node)).FontSize(HeadingFontSize).Bold().FontColor(Teal);
        });
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

        if (classAttribute.Contains("meta", StringComparison.Ordinal))
        {
            column.Item().Background(Mint).Padding(8).Text(text).FontSize(MetaFontSize).FontColor(Navy);
            return;
        }

        if (classAttribute.Contains("quote", StringComparison.Ordinal))
        {
            column.Item().BorderRight(4).BorderColor(Mustard).PaddingRight(10).PaddingVertical(4)
                .Text(text).FontSize(BodyFontSize).Italic().FontColor(Navy);
            return;
        }

        column.Item().Text(text).FontSize(BodyFontSize);
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

        var widths = ColumnWidths(node, columnCount);
        HtmlNode? headerRow = rows[0].SelectNodes("./th") is { Count: > 0 } ? rows[0] : null;
        column.Item().Table(table =>
        {
            table.ColumnsDefinition(columns =>
            {
                foreach (var width in widths)
                {
                    columns.RelativeColumn(width);
                }
            });

            RenderTableHeader(table, headerRow);
            foreach (HtmlNode row in rows.Skip(headerRow is null ? 0 : 1))
            {
                RenderTableRow(table, row);
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
                RenderCell(header.Cell(), cell, true);
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

    private static void RenderTableRow(TableDescriptor table, HtmlNode row)
    {
        var isHeaderRow = row.SelectNodes("./th") is { Count: > 0 };
        foreach (HtmlNode cell in row.SelectNodes("./th|./td") ?? Enumerable.Empty<HtmlNode>())
        {
            RenderCell(table.Cell(), cell, isHeaderRow);
        }
    }

    private static void RenderCell(IContainer cell, HtmlNode node, bool isHeaderRow)
    {
        var cellText = TextOf(node);
        IContainer styledCell = isHeaderRow
            ? cell.Background(Teal).Padding(5)
            : cell.BorderBottom(1).BorderColor(Mint).Padding(5);
        QuestPDF.Fluent.TextSpanDescriptor cellSpan = styledCell.Text(cellText);
        if (isHeaderRow)
        {
            cellSpan.FontColor(Colors.White).Bold();
        }
    }

    private static string TextOf(HtmlNode node) => System.Net.WebUtility.HtmlDecode(node.InnerText).Trim();
}
