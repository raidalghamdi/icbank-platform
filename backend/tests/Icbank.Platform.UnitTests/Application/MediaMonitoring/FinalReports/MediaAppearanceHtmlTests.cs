using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Appearance;
using Icbank.Platform.Domain.MediaMonitoring;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>
/// Verifies the exported document reports measured appearance figures and never prints a table of
/// zeros for platform metrics the model was not allowed to estimate.
/// </summary>
public sealed class MediaAppearanceHtmlTests
{
    [Fact]
    public void Build_WithMeasuredAppearance_PrintsCountedFiguresAndTopOutlets()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        var appearance = new MediaAppearanceAnalysisDto(
            70,
            70,
            0,
            12,
            22,
            3.2,
            "2026-08-17",
            9,
            [new MediaAppearanceOutletDto("معلومات مباشر", 10, 14)],
            [new MediaAppearanceDayDto("2026-08-17", 9)],
            [],
            false);

        var html = FinalReportHtmlBuilder.Build(FinalMediaReportMapper.ToDetailDto(report, appearance));

        html.Should().Contain("قياس الظهور الإعلامي خلال الفترة");
        html.Should().Contain("70");
        html.Should().Contain("3.2");
        html.Should().Contain("2026-08-17");
        html.Should().Contain("معلومات مباشر");
        html.Should().Contain("لا يوجد مصدر رصد مرتبط");
    }

    [Fact]
    public void Build_WithoutMeasuredAppearance_OmitsTheMeasuredSection()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);

        var html = FinalReportHtmlBuilder.Build(FinalMediaReportMapper.ToDetailDto(report));

        html.Should().NotContain("قياس الظهور الإعلامي خلال الفترة");
    }

    [Fact]
    public void Build_PlatformRowsAllZero_AreNotPrintedAsAnAnalysisOfZeros()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        report.DigitalPresence!.Platforms =
        [
            new DigitalPresencePlatform { Name = "إكس", Mentions = 0, Reposts = 0, Engagement = 0, Reach = "0" },
        ];
        report.DigitalPresence.Hashtags = [new DigitalPresenceHashtag { Tag = "#تجربة", Uses = 0, Trend = "ثابت" }];

        var html = FinalReportHtmlBuilder.Build(FinalMediaReportMapper.ToDetailDto(report));

        html.Should().NotContain("الحضور الرقمي");
        html.Should().NotContain("الوسوم الأكثر استخداماً");
    }

    [Fact]
    public void Build_WithMeasuredAppearance_PrefersCountedTotalsOverTheStoredEstimate()
    {
        FinalMediaReport report = FinalMediaReportTestData.BuildEntity(1);
        report.Kpis!.TotalNews = 999;
        report.Kpis.MediaOutlets = 888;
        var appearance = new MediaAppearanceAnalysisDto(70, 70, 0, 12, 22, 3.2, "2026-08-17", 9, [], [], [], false);

        var html = FinalReportHtmlBuilder.Build(FinalMediaReportMapper.ToDetailDto(report, appearance));

        html.Should().NotContain("999");
        html.Should().NotContain("888");
        html.Should().Contain("خبر منشور");
    }
}
