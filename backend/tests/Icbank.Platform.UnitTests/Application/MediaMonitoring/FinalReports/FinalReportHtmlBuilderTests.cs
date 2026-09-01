using System.Text.RegularExpressions;
using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Domain.MediaMonitoring;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>
/// Verifies <see cref="FinalReportHtmlBuilder"/> HTML-encodes every untrusted value it places in
/// the document (closes the H-1 class of defect: never interpolate untrusted content into
/// markup).
/// </summary>
public sealed class FinalReportHtmlBuilderTests
{
    [Fact]
    public void Build_TitleContainsHtmlMarkup_IsEncodedNotInterpolatedRaw()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        report.Title = "<script>alert(1)</script>";
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);

        var html = FinalReportHtmlBuilder.Build(detail);

        html.Should().NotContain("<script>alert(1)</script>");
        html.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void Build_Always_IncludesReportNumberAndPeriodLabel()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1, "GAC-MEDIA-7/2026");
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);

        var html = FinalReportHtmlBuilder.Build(detail);

        html.Should().Contain("GAC-MEDIA-7/2026");
        html.Should().Contain(report.PeriodLabel);
    }

    [Fact]
    public void Build_EmptyTopNews_RendersNoNewsMessageInsteadOfEmptyTable()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        report.TopNews.Clear();
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);

        var html = FinalReportHtmlBuilder.Build(detail);

        html.Should().Contain("لا توجد أخبار مسجلة.");
    }

    [Fact]
    public void Build_NullExecutiveSummary_RendersEmptyParagraphNotNullText()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        report.ExecutiveSummary = null;
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);

        var html = FinalReportHtmlBuilder.Build(detail);

        html.Should().NotContain("null");
    }

    /// <summary>
    /// A reader who opens the exported report should not have to go back to the screen to see the
    /// timeline, the tone breakdown, the analysis, the alerts, the quotes or the sources.
    /// </summary>
    [Fact]
    public void Build_FullReport_CarriesEverySectionTheReportHolds()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);

        var html = FinalReportHtmlBuilder.Build(detail);

        var expectedSections = new[]
        {
            "الملخص التنفيذي", "المؤشرات الإعلامية الرئيسية", "أبرز الأخبار خلال الفترة",
            "الخط الزمني للتغطية", "تحليل التوجه الإعلامي", "الحضور الرقمي", "الوسوم الأكثر استخداماً",
            "توزع نبرة التغطية الإعلامية", "التصنيف الموضوعي للأخبار", "توزع التغطية حسب المصدر",
            "تحليل عميق ومؤشرات قطاعية", "أبرز الكلمات المفتاحية في التغطية", "اقتباس بارز من التغطية",
            "قراءة استراتيجية للحضور الإعلامي", "المقارنة الإقليمية", "التوصيات والإجراءات المقترحة",
            "تنبيهات تستوجب المتابعة", "المنهجية والمصادر", "منهجية الرصد", "ملحق التصريحات",
            "المصادر الرئيسية المعتمدة",
        };
        expectedSections.Where(section => !html.Contains(section, StringComparison.Ordinal)).Should().BeEmpty();
    }

    [Fact]
    public void Build_FullReport_CarriesTheValuesOfEverySection()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);

        var html = FinalReportHtmlBuilder.Build(detail);

        var expectedValues = new[] { "حدث", "#تجربة", "المنافسة", "اقتباس", "تنبيه", "موقف", "منهجية", "https://example.com" };
        expectedValues.Where(value => !html.Contains(value, StringComparison.Ordinal)).Should().BeEmpty();
    }

    /// <summary>
    /// Equal column shares squeezed a headline into a sliver beside a date column that needed a
    /// fraction of the width it was given, so every table declares the share each column needs.
    /// </summary>
    [Fact]
    public void Build_EveryTable_DeclaresAColumnWidthPerColumn()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);

        var html = FinalReportHtmlBuilder.Build(detail);

        MatchCollection tables = Regex.Matches(html, "<table data-widths=\"([^\"]+)\"><tr>(.+?)</tr>", RegexOptions.Singleline);
        tables.Should().NotBeEmpty();
        tables.Where(table =>
            table.Groups[1].Value.Split(',').Length != Regex.Matches(table.Groups[2].Value, "<th").Count)
            .Should().BeEmpty();
    }

    /// <summary>
    /// The exported report has to be recognisable as the authority's own document: a cover sheet
    /// carrying the authority's identity and the report's identification, then the six numbered
    /// sections of the approved media-monitoring layout.
    /// </summary>
    [Fact]
    public void Build_Always_OpensWithTheAuthorityCoverSheet()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1, "GAC-MEDIA-7/2026");
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);

        var html = FinalReportHtmlBuilder.Build(detail);

        html.Should().Contain("<div class=\"cover\"");
        html.Should().Contain("data-org=\"الهيئة العامة للمنافسة\"");
        html.Should().Contain("General Authority for Competition");
        html.Should().Contain("data-report-number=\"GAC-MEDIA-7/2026\"");
        html.Should().Contain("سري — للاستخدام الداخلي");
        html.Should().Contain("الجهة المعدة");
    }

    [Fact]
    public void Build_Always_NumbersTheSixSectionsOfTheApprovedLayout()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);

        var html = FinalReportHtmlBuilder.Build(detail);

        Enumerable.Range(1, 6)
            .Where(number => !html.Contains(
                "<h2 data-number=\"" + number.ToString(System.Globalization.CultureInfo.InvariantCulture) + "\">",
                StringComparison.Ordinal))
            .Should().BeEmpty();
    }

    /// <summary>
    /// The period's news are the body of the report, not four columns of a table: each entry keeps
    /// its headline, its date and tone, its detail lines and its source.
    /// </summary>
    [Fact]
    public void Build_TopNews_RendersEachItemAsAnEntryWithItsOwnSourceLine()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        FinalMediaReportDetailDto detail = FinalMediaReportMapper.ToDetailDto(report);

        var html = FinalReportHtmlBuilder.Build(detail);

        html.Should().Contain("<div class=\"news-item\" data-index=\"1\">");
        html.Should().Contain("class=\"news-headline\"");
        html.Should().Contain("class=\"news-source\"");
        html.Should().Contain("class=\"kpi-grid\"");
    }
}
