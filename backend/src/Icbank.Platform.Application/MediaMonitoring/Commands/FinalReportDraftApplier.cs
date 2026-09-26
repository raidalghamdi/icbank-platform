using Icbank.Platform.Domain.MediaMonitoring;

namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>
/// Copies a report draft's content onto a <see cref="FinalMediaReport"/>. Shared by creation and
/// by the Review &amp; Edit save so an edited draft is stored exactly the way a generated one is.
/// </summary>
public static class FinalReportDraftApplier
{
    /// <summary>Overwrites the report's content fields with the draft's and refreshes the content hash.</summary>
    /// <param name="report">The report to update.</param>
    /// <param name="draft">The draft content.</param>
    public static void Apply(FinalMediaReport report, FinalReportDraftDto draft)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(draft);
        report.ExecutiveSummary = draft.ExecutiveSummary;
        report.Kpis = new ReportKpis { TotalNews = draft.Kpis.TotalNews, PositivePercent = draft.Kpis.PositivePercent, MediaOutlets = draft.Kpis.MediaOutlets, KeyTopics = draft.Kpis.KeyTopics, Reach = draft.Kpis.Reach, AlertsCount = draft.Kpis.AlertsCount };
        report.TopNews = draft.TopNews.Select(n => new TopNewsItem { Date = n.Date, Tone = n.Tone, Headline = n.Headline, Details = n.Details.ToList(), Source = n.Source }).ToList();
        report.Timeline = draft.Timeline.Select(t => new TimelineEvent { Date = t.Date, Event = t.Event, Outlet = t.Outlet, Tone = t.Tone, Count = t.Count }).ToList();
        report.DigitalPresence = BuildDigitalPresence(draft.DigitalPresence);
        report.EditorialTone = BuildEditorialTone(draft.EditorialTone);
        report.DeepAnalysis = BuildDeepAnalysis(draft.DeepAnalysis);
        report.RegionalComparison = draft.RegionalComparison.Select(r => new RegionalComparison { Authority = r.Authority, Country = r.Country, Mentions = r.Mentions, Tone = r.Tone, Highlights = r.Highlights }).ToList();
        report.Recommendations = draft.Recommendations.Select(r => new Recommendation { Title = r.Title, Description = r.Description, Priority = r.Priority, Responsible = r.Responsible, Kpi = r.Kpi, Deadline = r.Deadline, Dependencies = r.Dependencies }).ToList();
        report.Alerts = draft.Alerts.Select(a => new AlertItem { Alert = a.Alert, SuggestedPosition = a.SuggestedPosition }).ToList();
        report.QuotesAppendix = draft.QuotesAppendix.Select(q => new QuoteAppendixItem { Quote = q.Quote, Source = q.Source, Date = q.Date, Topic = q.Topic }).ToList();
        report.Methodology = draft.Methodology;
        report.Sources = draft.Sources.Select(s => new SourceRef { Name = s.Name, Url = s.Url, Description = s.Description }).ToList();
        report.ContentSha256 = FinalReportContentHasher.ComputeSha256(draft);
    }

    private static DigitalPresence BuildDigitalPresence(DigitalPresenceDto dto) => new()
    {
        Platforms = dto.Platforms.Select(p => new DigitalPresencePlatform { Name = p.Name, Mentions = p.Mentions, Reposts = p.Reposts, Engagement = p.Engagement, Reach = p.Reach }).ToList(),
        Hashtags = dto.Hashtags.Select(h => new DigitalPresenceHashtag { Tag = h.Tag, Uses = h.Uses, Trend = h.Trend }).ToList(),
    };

    private static EditorialTone BuildEditorialTone(EditorialToneDto dto) => new()
    {
        Distribution = dto.Distribution.Select(ToBucket).ToList(),
        Classification = dto.Classification.Select(ToBucket).ToList(),
        Sources = dto.Sources.Select(ToBucket).ToList(),
    };

    private static EditorialToneBucket ToBucket(EditorialToneBucketDto dto) => new() { Label = dto.Label, Percent = dto.Percent, Count = dto.Count };

    private static DeepAnalysis BuildDeepAnalysis(DeepAnalysisDto dto) => new()
    {
        Keywords = dto.Keywords.Select(k => new DeepAnalysisKeyword { Keyword = k.Keyword, Frequency = k.Frequency, Context = k.Context }).ToList(),
        Quote = dto.Quote is null ? null : new DeepAnalysisQuote { Text = dto.Quote.Text, Source = dto.Quote.Source, Date = dto.Quote.Date },
        Strengths = dto.Strengths.ToList(),
        Weaknesses = dto.Weaknesses.ToList(),
    };
}
