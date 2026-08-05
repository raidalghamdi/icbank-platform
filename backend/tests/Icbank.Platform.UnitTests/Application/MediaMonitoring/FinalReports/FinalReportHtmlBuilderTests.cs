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
}
