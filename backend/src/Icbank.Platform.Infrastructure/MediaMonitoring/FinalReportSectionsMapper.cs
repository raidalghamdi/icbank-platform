using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Domain.MediaMonitoring;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>Maps the raw §5.3 JSON wire DTO (<see cref="FinalReportSectionsJsonDto"/>) onto the persisted <see cref="FinalReportSections"/> CLR shape.</summary>
public static class FinalReportSectionsMapper
{
    /// <summary>Maps a parsed wire DTO to the persisted sections shape, defaulting every absent field to its documented empty value.</summary>
    /// <param name="dto">The parsed JSON DTO.</param>
    /// <returns>The mapped <see cref="FinalReportSections"/>.</returns>
    public static FinalReportSections Map(FinalReportSectionsJsonDto dto) => new()
    {
        ExecutiveSummary = dto.ExecutiveSummary ?? string.Empty,
        Kpis = MapKpis(dto.Kpis),
        TopNews = (dto.TopNews ?? []).Select(MapTopNews).ToList(),
        Timeline = (dto.Timeline ?? []).Select(MapTimeline).ToList(),
        DigitalPresence = MapDigitalPresence(dto.DigitalPresence),
        EditorialTone = MapEditorialTone(dto.EditorialTone),
        DeepAnalysis = MapDeepAnalysis(dto.DeepAnalysis),
        RegionalComparison = (dto.RegionalComparison ?? []).Select(MapRegional).ToList(),
        Recommendations = (dto.Recommendations ?? []).Select(MapRecommendation).ToList(),
        Alerts = (dto.Alerts ?? []).Select(MapAlert).ToList(),
        QuotesAppendix = (dto.QuotesAppendix ?? []).Select(MapQuoteAppendix).ToList(),
        Methodology = dto.Methodology ?? string.Empty,
        Sources = (dto.Sources ?? []).Select(MapSource).ToList(),
    };

    private static ReportKpis MapKpis(KpisDto? dto) => dto is null
        ? new ReportKpis()
        : new ReportKpis
        {
            TotalNews = dto.TotalNews,
            PositivePercent = dto.PositivePercent,
            MediaOutlets = dto.MediaOutlets,
            KeyTopics = dto.KeyTopics,
            Reach = dto.Reach,
            AlertsCount = dto.AlertsCount,
        };

    private static TopNewsItem MapTopNews(TopNewsDto dto) => new()
    {
        Date = dto.Date ?? string.Empty,
        Tone = dto.Tone ?? string.Empty,
        Headline = dto.Headline ?? string.Empty,
        Details = dto.Details ?? [],
        Source = dto.Source ?? string.Empty,
    };

    private static TimelineEvent MapTimeline(TimelineDto dto) => new()
    {
        Date = dto.Date ?? string.Empty,
        Event = dto.Event ?? string.Empty,
        Outlet = dto.Outlet ?? string.Empty,
        Tone = dto.Tone ?? string.Empty,
        Count = dto.Count ?? 0,
    };

    private static DigitalPresence MapDigitalPresence(DigitalPresenceDto? dto) => new()
    {
        Platforms = (dto?.Platforms ?? []).Select(p => new DigitalPresencePlatform
        {
            Name = p.Name ?? string.Empty,
            Mentions = p.Mentions ?? 0,
            Reposts = p.Reposts ?? 0,
            Engagement = p.Engagement ?? 0,
            Reach = p.Reach ?? string.Empty,
        }).ToList(),
        Hashtags = (dto?.Hashtags ?? []).Select(h => new DigitalPresenceHashtag
        {
            Tag = h.Tag ?? string.Empty,
            Uses = h.Uses ?? 0,
            Trend = h.Trend ?? string.Empty,
        }).ToList(),
    };

    private static EditorialTone MapEditorialTone(EditorialToneDto? dto) => new()
    {
        Distribution = MapBuckets(dto?.Distribution),
        Classification = MapBuckets(dto?.Classification),
        Sources = MapBuckets(dto?.Sources),
    };

    private static List<EditorialToneBucket> MapBuckets(List<ToneBucketDto>? buckets) =>
        (buckets ?? []).Select(b => new EditorialToneBucket { Label = b.Label ?? string.Empty, Percent = b.Percent ?? 0, Count = b.Count ?? 0 }).ToList();

    private static DeepAnalysis MapDeepAnalysis(DeepAnalysisDto? dto) => new()
    {
        Keywords = (dto?.Keywords ?? []).Select(k => new DeepAnalysisKeyword
        {
            Keyword = k.Keyword ?? string.Empty,
            Frequency = k.Frequency ?? 0,
            Context = k.Context ?? string.Empty,
        }).ToList(),
        Quote = dto?.Quote is null ? null : new DeepAnalysisQuote { Text = dto.Quote.Text ?? string.Empty, Source = dto.Quote.Source ?? string.Empty, Date = dto.Quote.Date ?? string.Empty },
        Strengths = dto?.Strengths ?? [],
        Weaknesses = dto?.Weaknesses ?? [],
    };

    private static RegionalComparison MapRegional(RegionalComparisonDto dto) => new()
    {
        Authority = dto.Authority ?? string.Empty,
        Country = dto.Country ?? string.Empty,
        Mentions = dto.Mentions ?? 0,
        Tone = dto.Tone ?? string.Empty,
        Highlights = dto.Highlights ?? string.Empty,
    };

    private static Recommendation MapRecommendation(RecommendationDto dto) => new()
    {
        Title = dto.Title ?? string.Empty,
        Description = dto.Description ?? string.Empty,
        Priority = dto.Priority ?? string.Empty,
        Responsible = dto.Responsible ?? string.Empty,
        Kpi = dto.Kpi ?? string.Empty,
        Deadline = dto.Deadline ?? string.Empty,
        Dependencies = dto.Dependencies ?? string.Empty,
    };

    private static AlertItem MapAlert(AlertDto dto) => new() { Alert = dto.Alert ?? string.Empty, SuggestedPosition = dto.SuggestedPosition ?? string.Empty };

    private static QuoteAppendixItem MapQuoteAppendix(QuoteAppendixDto dto) => new()
    {
        Quote = dto.Quote ?? string.Empty,
        Source = dto.Source ?? string.Empty,
        Date = dto.Date ?? string.Empty,
        Topic = dto.Topic ?? string.Empty,
    };

    private static SourceRef MapSource(SourceRefDto dto) => new() { Name = dto.Name ?? string.Empty, Url = dto.Url ?? string.Empty, Description = dto.Description };
}
