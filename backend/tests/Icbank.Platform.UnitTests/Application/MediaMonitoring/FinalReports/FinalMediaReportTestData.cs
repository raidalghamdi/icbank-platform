using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Application.MediaMonitoring.Commands;
using Icbank.Platform.Domain.MediaMonitoring;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring.FinalReports;

/// <summary>Shared builders for final-media-report test fixtures, used by every handler/validator test in this folder.</summary>
public static class FinalMediaReportTestData
{
    /// <summary>Builds a minimal-but-complete <see cref="FinalMediaReport"/> entity for handler tests.</summary>
    /// <param name="id">The entity id.</param>
    /// <param name="reportNumber">The official report number.</param>
    public static FinalMediaReport BuildEntity(int id = 1, string reportNumber = "GAC-MEDIA-1/2026") => new()
    {
        Id = id,
        ReportNumber = reportNumber,
        Title = "تقرير الرصد الإعلامي",
        ReportType = MediaReportType.Weekly,
        PeriodLabel = "الأسبوع الأول من يوليو 2026",
        DateFrom = new DateTimeOffset(2026, 7, 1, 0, 0, 0, TimeSpan.Zero),
        DateTo = new DateTimeOffset(2026, 7, 7, 0, 0, 0, TimeSpan.Zero),
        IssueDate = new DateTimeOffset(2026, 7, 8, 0, 0, 0, TimeSpan.Zero),
        ExecutiveSummary = "ملخص تنفيذي تجريبي",
        Kpis = new ReportKpis { TotalNews = 10, PositivePercent = 70, MediaOutlets = 5, KeyTopics = 3, Reach = "1.2 م", AlertsCount = 1 },
        TopNews = new List<TopNewsItem> { new() { Date = "2026-07-01", Tone = "إيجابي", Headline = "خبر", Details = new List<string> { "تفصيل" }, Source = "مصدر" } },
        Timeline = new List<TimelineEvent> { new() { Date = "2026-07-01", Event = "حدث", Outlet = "وسيلة", Tone = "محايد", Count = 2 } },
        DigitalPresence = new DigitalPresence
        {
            Platforms = new List<DigitalPresencePlatform> { new() { Name = "إكس", Mentions = 5, Reposts = 1, Engagement = 10, Reach = "1 م" } },
            Hashtags = new List<DigitalPresenceHashtag> { new() { Tag = "#تجربة", Uses = 3, Trend = "ثابت" } },
        },
        EditorialTone = new EditorialTone
        {
            Distribution = new List<EditorialToneBucket> { new() { Label = "إيجابي", Percent = 70, Count = 7 } },
            Classification = new List<EditorialToneBucket> { new() { Label = "قرارات", Percent = 50, Count = 5 } },
            Sources = new List<EditorialToneBucket> { new() { Label = "صحف", Percent = 40, Count = 4 } },
        },
        DeepAnalysis = new DeepAnalysis
        {
            Keywords = new List<DeepAnalysisKeyword> { new() { Keyword = "المنافسة", Frequency = 5, Context = "سياق" } },
            Quote = new DeepAnalysisQuote { Text = "اقتباس", Source = "مصدر", Date = "2026-07-01" },
            Strengths = new List<string> { "قوة" },
            Weaknesses = new List<string> { "ضعف" },
        },
        RegionalComparison = new List<RegionalComparison> { new() { Authority = "هيئة", Country = "دولة", Mentions = 2, Tone = "محايد", Highlights = "أبرز" } },
        Recommendations = new List<Recommendation>
        {
            new() { Title = "توصية", Description = "وصف", Priority = "عالية", Responsible = "جهة", Kpi = "مؤشر", Deadline = "أسبوعين", Dependencies = "لا شيء" },
        },
        Alerts = new List<AlertItem> { new() { Alert = "تنبيه", SuggestedPosition = "موقف" } },
        QuotesAppendix = new List<QuoteAppendixItem> { new() { Quote = "اقتباس", Source = "مصدر", Date = "2026-07-01", Topic = "موضوع" } },
        Methodology = "منهجية",
        Sources = new List<SourceRef> { new() { Name = "مصدر", Url = "https://example.com", Description = "وصف" } },
        SourceItemsJson = "[]",
        Status = FinalMediaReportStatus.Final,
        ViewCount = 0,
        ContentSha256 = "abc123",
    };

    /// <summary>Builds a minimal-but-complete <see cref="FinalReportDraftDto"/> for command tests.</summary>
    public static FinalReportDraftDto BuildDraftDto() => new(
        "الأسبوع الأول من يوليو 2026",
        "ملخص تنفيذي تجريبي",
        new ReportKpisDto(10, 70, 5, 3, "1.2 م", 1),
        new List<TopNewsItemDto> { new("2026-07-01", "إيجابي", "خبر", new List<string> { "تفصيل" }, "مصدر") },
        new List<TimelineEventDto> { new("2026-07-01", "حدث", "وسيلة", "محايد", 2) },
        new DigitalPresenceDto(
            new List<DigitalPresencePlatformDto> { new("إكس", 5, 1, 10, "1 م") },
            new List<DigitalPresenceHashtagDto> { new("#تجربة", 3, "ثابت") }),
        new EditorialToneDto(
            new List<EditorialToneBucketDto> { new("إيجابي", 70, 7) },
            new List<EditorialToneBucketDto> { new("قرارات", 50, 5) },
            new List<EditorialToneBucketDto> { new("صحف", 40, 4) }),
        new DeepAnalysisDto(
            new List<DeepAnalysisKeywordDto> { new("المنافسة", 5, "سياق") },
            new DeepAnalysisQuoteDto("اقتباس", "مصدر", "2026-07-01"),
            new List<string> { "قوة" },
            new List<string> { "ضعف" }),
        new List<RegionalComparisonDto> { new("هيئة", "دولة", 2, "محايد", "أبرز") },
        new List<RecommendationDto> { new("توصية", "وصف", "عالية", "جهة", "مؤشر", "أسبوعين", "لا شيء") },
        new List<AlertItemDto> { new("تنبيه", "موقف") },
        new List<QuoteAppendixItemDto> { new("اقتباس", "مصدر", "2026-07-01", "موضوع") },
        "منهجية",
        new List<SourceRefDto> { new("مصدر", "https://example.com", "وصف") });
}
