namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>Full read model for a single final media report, including every one of the 8 report sections.</summary>
/// <param name="Summary">The list-shape summary fields shared with <see cref="FinalMediaReportDto"/>.</param>
/// <param name="TopNews">The top news items (section 2).</param>
/// <param name="Timeline">The detailed timeline (section 3).</param>
/// <param name="DigitalPresence">The digital-presence analysis (section 4).</param>
/// <param name="EditorialTone">The editorial-tone analysis (section 5).</param>
/// <param name="DeepAnalysis">The deep sectoral analysis (section 6).</param>
/// <param name="RegionalComparison">The regional comparison table (section 7).</param>
/// <param name="Recommendations">The recommendations and action plan (section 8a).</param>
/// <param name="Alerts">The alerts and suggested positions (section 8b).</param>
/// <param name="QuotesAppendix">The quotes appendix.</param>
/// <param name="Methodology">The methodology notes.</param>
/// <param name="Sources">The source list.</param>
public sealed record FinalMediaReportDetailDto(
    FinalMediaReportDto Summary,
    IReadOnlyList<TopNewsItemDto> TopNews,
    IReadOnlyList<TimelineEventDto> Timeline,
    DigitalPresenceDto DigitalPresence,
    EditorialToneDto EditorialTone,
    DeepAnalysisDto DeepAnalysis,
    IReadOnlyList<RegionalComparisonDto> RegionalComparison,
    IReadOnlyList<RecommendationDto> Recommendations,
    IReadOnlyList<AlertItemDto> Alerts,
    IReadOnlyList<QuoteAppendixItemDto> QuotesAppendix,
    string? Methodology,
    IReadOnlyList<SourceRefDto> Sources);
