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
            "مؤشرات الفترة", "الملخص التنفيذي", "أبرز الأخبار", "الخط الزمني للتغطية", "الحضور الرقمي",
            "الوسوم الأكثر استخداماً", "توزيع النبرة التحريرية", "تصنيف التغطية", "النبرة حسب المصدر",
            "الكلمات المفتاحية", "تصريح بارز", "المقارنة الإقليمية", "التوصيات",
            "التنبيهات والموقف المقترح", "ملحق التصريحات", "المنهجية", "المصادر",
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
            table.Groups[1].Value.Split(',').Length != Regex.Matches(table.Groups[2].Value, "<th>").Count)
            .Should().BeEmpty();
    }
}
