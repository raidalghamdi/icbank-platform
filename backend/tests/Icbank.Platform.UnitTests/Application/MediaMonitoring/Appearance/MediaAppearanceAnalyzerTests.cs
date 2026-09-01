using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring.Appearance;
using Icbank.Platform.Domain.Gac;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.Appearance;

/// <summary>
/// Covers the appearance measurement that replaced the model-authored zeros in section four.
/// </summary>
public sealed class MediaAppearanceAnalyzerTests
{
    [Fact]
    public void Analyze_WithNoMonitoredRows_ReturnsEmptyAnalysis()
    {
        MediaAppearanceAnalysisDto result = MediaAppearanceAnalyzer.Analyze([], []);

        result.TotalAppearances.Should().Be(0);
        result.DistinctOutlets.Should().Be(0);
        result.ActiveDays.Should().Be(0);
        result.PeakDay.Should().BeNull();
        result.HasSocialData.Should().BeFalse();
        result.TopOutlets.Should().BeEmpty();
        result.DailyTrend.Should().BeEmpty();
        result.Platforms.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_CountsPressAppearancesPerOutlet()
    {
        List<GacNewsItem> news =
        [
            NewsItem("معلومات مباشر", "2026-08-02T09:00:00+03:00"),
            NewsItem("معلومات مباشر", "2026-08-02T13:00:00+03:00"),
            NewsItem("صحيفة مال", "2026-08-03T08:00:00+03:00"),
            NewsItem("أرقام", "2026-08-04T08:00:00+03:00"),
        ];

        MediaAppearanceAnalysisDto result = MediaAppearanceAnalyzer.Analyze(news, []);

        result.TotalAppearances.Should().Be(4);
        result.PressAppearances.Should().Be(4);
        result.SocialAppearances.Should().Be(0);
        result.DistinctOutlets.Should().Be(3);
        result.TopOutlets[0].Name.Should().Be("معلومات مباشر");
        result.TopOutlets[0].Appearances.Should().Be(2);
        result.TopOutlets[0].SharePercent.Should().Be(50);
    }

    [Fact]
    public void Analyze_BreaksOutletTiesByName()
    {
        List<GacNewsItem> news =
        [
            NewsItem("باء", "2026-08-02T09:00:00+03:00"),
            NewsItem("ألف", "2026-08-02T09:00:00+03:00"),
        ];

        MediaAppearanceAnalysisDto result = MediaAppearanceAnalyzer.Analyze(news, []);

        result.TopOutlets.Select(o => o.Name).Should().ContainInOrder("ألف", "باء");
    }

    [Fact]
    public void Analyze_FallsBackToSourceHostThenUnknownLabel()
    {
        List<GacNewsItem> news =
        [
            new() { TitleAr = "خبر", ExternalRef = null, SourceUrl = "https://www.maaal.com/a", PublishedAt = Stamp("2026-08-02T09:00:00+03:00") },
            new() { TitleAr = "خبر", ExternalRef = null, SourceUrl = null, PublishedAt = Stamp("2026-08-02T10:00:00+03:00") },
        ];

        MediaAppearanceAnalysisDto result = MediaAppearanceAnalyzer.Analyze(news, []);

        result.TopOutlets.Select(o => o.Name).Should().Contain("maaal.com").And.Contain("مصدر غير محدد");
    }

    [Fact]
    public void Analyze_BucketsDaysByRiyadhTimeNotUtc()
    {
        // 2026-08-02T22:30Z is already 2026-08-03 in Riyadh, which is the calendar the report is read in.
        List<GacNewsItem> news =
        [
            NewsItem("معلومات مباشر", "2026-08-02T22:30:00+00:00"),
            NewsItem("معلومات مباشر", "2026-08-03T05:00:00+03:00"),
        ];

        MediaAppearanceAnalysisDto result = MediaAppearanceAnalyzer.Analyze(news, []);

        result.DailyTrend.Should().HaveCount(1);
        result.DailyTrend[0].Date.Should().Be("2026-08-03");
        result.DailyTrend[0].Appearances.Should().Be(2);
        result.ActiveDays.Should().Be(1);
        result.PeakDay.Should().Be("2026-08-03");
        result.PeakDayAppearances.Should().Be(2);
    }

    [Fact]
    public void Analyze_OrdersDailyTrendChronologically()
    {
        List<GacNewsItem> news =
        [
            NewsItem("معلومات مباشر", "2026-08-10T09:00:00+03:00"),
            NewsItem("معلومات مباشر", "2026-08-02T09:00:00+03:00"),
            NewsItem("معلومات مباشر", "2026-08-05T09:00:00+03:00"),
        ];

        MediaAppearanceAnalysisDto result = MediaAppearanceAnalyzer.Analyze(news, []);

        result.DailyTrend.Select(d => d.Date).Should().ContainInOrder("2026-08-02", "2026-08-05", "2026-08-10");
        result.AveragePerDay.Should().Be(1);
    }

    [Fact]
    public void Analyze_RoundsAveragePerDayToOneDecimalAwayFromZero()
    {
        List<GacNewsItem> news =
        [
            NewsItem("أ", "2026-08-02T09:00:00+03:00"),
            NewsItem("أ", "2026-08-02T10:00:00+03:00"),
            NewsItem("أ", "2026-08-03T09:00:00+03:00"),
            NewsItem("أ", "2026-08-04T09:00:00+03:00"),
            NewsItem("أ", "2026-08-04T10:00:00+03:00"),
        ];

        MediaAppearanceAnalysisDto result = MediaAppearanceAnalyzer.Analyze(news, []);

        result.AveragePerDay.Should().Be(1.7);
    }

    [Fact]
    public void Analyze_CapsTopOutletsAtTen()
    {
        var news = Enumerable.Range(1, 14)
            .Select(i => NewsItem("منفذ " + i.ToString("00", System.Globalization.CultureInfo.InvariantCulture), "2026-08-02T09:00:00+03:00"))
            .ToList();

        MediaAppearanceAnalysisDto result = MediaAppearanceAnalyzer.Analyze(news, []);

        result.DistinctOutlets.Should().Be(14);
        result.TopOutlets.Should().HaveCount(10);
    }

    [Fact]
    public void Analyze_ReportsSocialDataOnlyWhenPostsExist()
    {
        List<GacSocialPost> posts =
        [
            new()
            {
                Platform = GacSocialPlatform.Twitter,
                PostedAt = Stamp("2026-08-02T09:00:00+03:00"),
                Metrics = new SocialMetrics { Likes = 10, Comments = 4, Shares = 6 },
            },
            new()
            {
                Platform = GacSocialPlatform.Twitter,
                PostedAt = Stamp("2026-08-02T11:00:00+03:00"),
                Metrics = new SocialMetrics { Likes = 1, Comments = 1, Shares = 1 },
            },
            new()
            {
                Platform = GacSocialPlatform.LinkedIn,
                PostedAt = Stamp("2026-08-03T11:00:00+03:00"),
                Metrics = null,
            },
        ];

        MediaAppearanceAnalysisDto result = MediaAppearanceAnalyzer.Analyze([NewsItem("أرقام", "2026-08-02T09:00:00+03:00")], posts);

        result.HasSocialData.Should().BeTrue();
        result.SocialAppearances.Should().Be(3);
        result.TotalAppearances.Should().Be(4);
        result.Platforms[0].Name.Should().Be("إكس");
        result.Platforms[0].Posts.Should().Be(2);
        result.Platforms[0].Engagement.Should().Be(23);
        result.Platforms[0].Reposts.Should().Be(7);
        result.Platforms[1].Name.Should().Be("لينكدإن");
        result.Platforms[1].Engagement.Should().Be(0);
    }

    [Fact]
    public void Analyze_FallsBackToCreatedAtWhenPublishedAtMissing()
    {
        List<GacNewsItem> news =
        [
            new() { TitleAr = "خبر", ExternalRef = "أرقام", PublishedAt = null, CreatedAt = Stamp("2026-08-06T12:00:00+00:00").UtcDateTime },
        ];

        MediaAppearanceAnalysisDto result = MediaAppearanceAnalyzer.Analyze(news, []);

        result.DailyTrend[0].Date.Should().Be("2026-08-06");
    }

    private static GacNewsItem NewsItem(string outlet, string publishedAt) => new()
    {
        TitleAr = "خبر",
        ExternalRef = outlet,
        SourceUrl = "https://example.com/a",
        PublishedAt = Stamp(publishedAt),
    };

    private static DateTimeOffset Stamp(string value) =>
        DateTimeOffset.Parse(value, System.Globalization.CultureInfo.InvariantCulture);
}
