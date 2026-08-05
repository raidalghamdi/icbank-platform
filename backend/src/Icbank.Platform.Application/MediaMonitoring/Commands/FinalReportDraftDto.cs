namespace Icbank.Platform.Application.MediaMonitoring.Commands;

/// <summary>The generated 8-section report draft.</summary>
/// <param name="PeriodLabel">The human-readable period label.</param>
/// <param name="ExecutiveSummary">The executive summary (section 1).</param>
/// <param name="Kpis">The report's key performance indicators.</param>
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
public sealed record FinalReportDraftDto(
    string PeriodLabel,
    string ExecutiveSummary,
    ReportKpisDto Kpis,
    IReadOnlyList<TopNewsItemDto> TopNews,
    IReadOnlyList<TimelineEventDto> Timeline,
    DigitalPresenceDto DigitalPresence,
    EditorialToneDto EditorialTone,
    DeepAnalysisDto DeepAnalysis,
    IReadOnlyList<RegionalComparisonDto> RegionalComparison,
    IReadOnlyList<RecommendationDto> Recommendations,
    IReadOnlyList<AlertItemDto> Alerts,
    IReadOnlyList<QuoteAppendixItemDto> QuotesAppendix,
    string Methodology,
    IReadOnlyList<SourceRefDto> Sources);
