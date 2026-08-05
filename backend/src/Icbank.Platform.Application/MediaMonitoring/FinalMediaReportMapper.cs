using Icbank.Platform.Domain.MediaMonitoring;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Shared entity-to-DTO mapping for <see cref="FinalMediaReport"/>, used by every final-report query/command handler.</summary>
public static class FinalMediaReportMapper
{
    /// <summary>Maps a <see cref="FinalMediaReport"/> entity to its list-shape summary DTO.</summary>
    /// <param name="report">The entity to map.</param>
    /// <returns>The mapped <see cref="FinalMediaReportDto"/>.</returns>
    public static FinalMediaReportDto ToSummaryDto(FinalMediaReport report) => new(
        report.Id,
        report.ReportNumber,
        report.Title,
        report.ReportType.ToString(),
        report.PeriodLabel,
        report.DateFrom,
        report.DateTo,
        report.ExecutiveSummary,
        ToKpisDto(report.Kpis),
        report.Status.ToString(),
        report.ViewCount,
        report.ContentSha256,
        report.CreatedAt);

    /// <summary>Maps a <see cref="FinalMediaReport"/> entity to its full detail DTO, including every report section.</summary>
    /// <param name="report">The entity to map.</param>
    /// <returns>The mapped <see cref="FinalMediaReportDetailDto"/>.</returns>
    public static FinalMediaReportDetailDto ToDetailDto(FinalMediaReport report) => new(
        ToSummaryDto(report),
        report.TopNews.Select(n => new TopNewsItemDto(n.Date, n.Tone, n.Headline, n.Details, n.Source)).ToList(),
        report.Timeline.Select(t => new TimelineEventDto(t.Date, t.Event, t.Outlet, t.Tone, t.Count)).ToList(),
        ToDigitalPresenceDto(report.DigitalPresence),
        ToEditorialToneDto(report.EditorialTone),
        ToDeepAnalysisDto(report.DeepAnalysis),
        report.RegionalComparison.Select(r => new RegionalComparisonDto(r.Authority, r.Country, r.Mentions, r.Tone, r.Highlights)).ToList(),
        report.Recommendations.Select(r => new RecommendationDto(r.Title, r.Description, r.Priority, r.Responsible, r.Kpi, r.Deadline, r.Dependencies)).ToList(),
        report.Alerts.Select(a => new AlertItemDto(a.Alert, a.SuggestedPosition)).ToList(),
        report.QuotesAppendix.Select(q => new QuoteAppendixItemDto(q.Quote, q.Source, q.Date, q.Topic)).ToList(),
        report.Methodology,
        report.Sources.Select(s => new SourceRefDto(s.Name, s.Url, s.Description)).ToList());

    private static ReportKpisDto ToKpisDto(ReportKpis kpis) =>
        new(kpis.TotalNews, kpis.PositivePercent, kpis.MediaOutlets, kpis.KeyTopics, kpis.Reach, kpis.AlertsCount);

    private static DigitalPresenceDto ToDigitalPresenceDto(DigitalPresence presence) => new(
        presence.Platforms.Select(p => new DigitalPresencePlatformDto(p.Name, p.Mentions, p.Reposts, p.Engagement, p.Reach)).ToList(),
        presence.Hashtags.Select(h => new DigitalPresenceHashtagDto(h.Tag, h.Uses, h.Trend)).ToList());

    private static EditorialToneDto ToEditorialToneDto(EditorialTone tone) => new(
        tone.Distribution.Select(ToBucketDto).ToList(),
        tone.Classification.Select(ToBucketDto).ToList(),
        tone.Sources.Select(ToBucketDto).ToList());

    private static EditorialToneBucketDto ToBucketDto(EditorialToneBucket bucket) => new(bucket.Label, bucket.Percent, bucket.Count);

    private static DeepAnalysisDto ToDeepAnalysisDto(DeepAnalysis analysis) => new(
        analysis.Keywords.Select(k => new DeepAnalysisKeywordDto(k.Keyword, k.Frequency, k.Context)).ToList(),
        analysis.Quote is null ? null : new DeepAnalysisQuoteDto(analysis.Quote.Text, analysis.Quote.Source, analysis.Quote.Date),
        analysis.Strengths,
        analysis.Weaknesses);
}
