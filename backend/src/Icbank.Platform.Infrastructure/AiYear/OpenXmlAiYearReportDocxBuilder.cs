using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using Icbank.Platform.Application.AiYear;
using Icbank.Platform.Application.AiYear.Queries;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Infrastructure.Rendering;

namespace Icbank.Platform.Infrastructure.AiYear;

/// <summary>
/// Real <see cref="IAiYearReportDocxRenderer"/> implementation, reproducing the Node original's
/// <c>docx</c>-package document tree (<c>ai-year.ts:440-569</c>) via
/// <c>DocumentFormat.OpenXml</c>: H1 title, italic RTL date line (Asia/Riyadh "today" from
/// <see cref="IDateTimeProvider"/>, never <c>DateTime.Now</c>), "مقدمة" intro, "إحصائيات العام"
/// with 4 bold stat lines, a conditional "أبرز ٣ تفعيلات" section (only rendered when
/// <see cref="AiYearReportDataDto.TopByReach"/> is non-empty, matching the Node source's
/// conditional array-spread), and a full-width table of every activation. Closes the Wave 2
/// regression noted in WAVE2-PORT-NOTES.md item 16 (the endpoint had been returning
/// <see cref="AiYearReportDataDto"/> JSON instead of <c>.docx</c> bytes).
/// </summary>
public sealed class OpenXmlAiYearReportDocxBuilder : IAiYearReportDocxRenderer
{
    private const string TitleAr = "تقرير عام الذكاء الاصطناعي ٢٠٢٦";
    private const string IntroHeadingAr = "مقدمة";

    private const string IntroBodyAr =
        "يوثّق هذا التقرير جميع تفعيلات وأنشطة إدارة التواصل الداخلي خلال عام الذكاء الاصطناعي ٢٠٢٦، " +
        "ويستعرض الإحصائيات والتوزيعات الشهرية والتحليلات الكاملة.";

    private const string StatsHeadingAr = "إحصائيات العام";
    private const string Top3HeadingAr = "أبرز ٣ تفعيلات (بحسب الوصول)";
    private const string FullTableHeadingAr = "جدول التفعيلات الكامل";
    private const string EmDash = "—";

    private readonly IDateTimeProvider _dateTimeProvider;

    /// <summary>Initializes a new instance of the <see cref="OpenXmlAiYearReportDocxBuilder"/> class.</summary>
    /// <param name="dateTimeProvider">The Asia/Riyadh-aware clock port, used for the report's issue-date line.</param>
    public OpenXmlAiYearReportDocxBuilder(IDateTimeProvider dateTimeProvider)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    /// <inheritdoc />
    public async Task<byte[]> RenderAsync(AiYearReportDataDto report, CancellationToken cancellationToken)
    {
        DateTimeOffset issueDate = _dateTimeProvider.RiyadhNow;
        var docxBytes = await RenderingGuard.RunWithTimeoutAsync(() => Build(report, issueDate), cancellationToken);
        RenderingGuard.EnsureWithinLimit(docxBytes.LongLength, "Rendered AI Year report DOCX");
        return docxBytes;
    }

    private static byte[] Build(AiYearReportDataDto report, DateTimeOffset issueDate)
    {
        var elements = new List<OpenXmlCompositeElement>
        {
            OpenXmlRtlHelpers.BuildParagraph(TitleAr, bold: true, styleId: "Heading1"),
            BuildDateParagraph(issueDate),
            OpenXmlRtlHelpers.BuildParagraph(string.Empty),
            OpenXmlRtlHelpers.BuildParagraph(IntroHeadingAr, bold: true, styleId: "Heading2"),
            OpenXmlRtlHelpers.BuildParagraph(IntroBodyAr),
            OpenXmlRtlHelpers.BuildParagraph(string.Empty),
            OpenXmlRtlHelpers.BuildParagraph(StatsHeadingAr, bold: true, styleId: "Heading2"),
            OpenXmlRtlHelpers.BuildParagraph($"إجمالي التفعيلات: {report.TotalActivations}", bold: true),
            OpenXmlRtlHelpers.BuildParagraph($"إجمالي الصور/الوسائط: {report.TotalMedia}", bold: true),
            OpenXmlRtlHelpers.BuildParagraph($"عدد القنوات المستخدمة: {report.TotalChannels}", bold: true),
            OpenXmlRtlHelpers.BuildParagraph($"توزيع الأنواع: {FormatByType(report.ByType)}", bold: true),
            OpenXmlRtlHelpers.BuildParagraph(string.Empty),
        };

        if (report.TopByReach.Count > 0)
        {
            elements.Add(OpenXmlRtlHelpers.BuildParagraph(Top3HeadingAr, bold: true, styleId: "Heading2"));
            for (var i = 0; i < report.TopByReach.Count; i++)
            {
                elements.Add(BuildTop3Paragraph(i, report.TopByReach[i]));
            }

            elements.Add(OpenXmlRtlHelpers.BuildParagraph(string.Empty));
        }

        elements.Add(OpenXmlRtlHelpers.BuildParagraph(FullTableHeadingAr, bold: true, styleId: "Heading2"));
        elements.Add(BuildActivationsTable(report.Rows));

        return OpenXmlDocxPackageWriter.Build(elements);
    }

    private static Paragraph BuildDateParagraph(DateTimeOffset issueDate)
    {
        // Why: matches the Node source's `toLocaleDateString("ar-SA", { year: "numeric", month:
        // "long", day: "numeric" })` -- .NET's "ar-SA" culture renders the same long Gregorian
        // Arabic-numeral date form ("5 أغسطس 2026") when using the Gregorian calendar explicitly.
        var arabicCulture = new CultureInfo("ar-SA") { DateTimeFormat = { Calendar = new GregorianCalendar() } };
        var formattedDate = issueDate.ToString("d MMMM yyyy", arabicCulture);
        var run = new Run(OpenXmlRtlHelpers.BuildRtlRunProperties(italic: true), new Text($"تاريخ الإصدار: {formattedDate}") { Space = SpaceProcessingModeValues.Preserve });
        return new Paragraph(OpenXmlRtlHelpers.BuildRtlParagraphProperties(), run);
    }

    private static string FormatByType(IReadOnlyDictionary<string, int> byType) =>
        string.Join($" {EmDash} ", byType.Select(pair => $"{pair.Key} ({pair.Value})"));

    private static Paragraph BuildTop3Paragraph(int index, AiYearReportRowDto row)
    {
        var reachText = row.Reach is not null ? row.Reach.Value.ToString(CultureInfo.InvariantCulture) : EmDash;
        var channelsText = string.Join(" / ", row.Channels);
        var leadRun = new Run(
            OpenXmlRtlHelpers.BuildRtlRunProperties(bold: true),
            new Text($"{index + 1}. {row.Title} {EmDash} ") { Space = SpaceProcessingModeValues.Preserve });
        var detailRun = new Run(
            OpenXmlRtlHelpers.BuildRtlRunProperties(),
            new Text($"{row.MonthNameAr} · {channelsText} · وصول: {reachText}") { Space = SpaceProcessingModeValues.Preserve });
        return new Paragraph(OpenXmlRtlHelpers.BuildRtlParagraphProperties(), leadRun, detailRun);
    }

    private static Table BuildActivationsTable(IReadOnlyList<AiYearReportRowDto> rows)
    {
        var table = new Table();
        table.AppendChild(new TableProperties(new TableWidth { Type = TableWidthUnitValues.Pct, Width = "5000" }));

        var headerRow = new TableRow(
            OpenXmlRtlHelpers.BuildTableCell("#", bold: true),
            OpenXmlRtlHelpers.BuildTableCell("العنوان", bold: true),
            OpenXmlRtlHelpers.BuildTableCell("الشهر", bold: true),
            OpenXmlRtlHelpers.BuildTableCell("النوع", bold: true),
            OpenXmlRtlHelpers.BuildTableCell("القناة", bold: true),
            OpenXmlRtlHelpers.BuildTableCell("الوصول", bold: true));
        table.AppendChild(headerRow);

        for (var i = 0; i < rows.Count; i++)
        {
            AiYearReportRowDto row = rows[i];
            var reachText = row.Reach is not null ? row.Reach.Value.ToString(CultureInfo.InvariantCulture) : EmDash;
            var channelsText = string.Join(" / ", row.Channels);
            var dataRow = new TableRow(
                OpenXmlRtlHelpers.BuildTableCell((i + 1).ToString(CultureInfo.InvariantCulture)),
                OpenXmlRtlHelpers.BuildTableCell(row.Title),
                OpenXmlRtlHelpers.BuildTableCell(row.MonthNameAr),
                OpenXmlRtlHelpers.BuildTableCell(row.Type),
                OpenXmlRtlHelpers.BuildTableCell(channelsText),
                OpenXmlRtlHelpers.BuildTableCell(reachText));
            table.AppendChild(dataRow);
        }

        return table;
    }
}
