using System.Text.Json.Serialization;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Wire-shape DTO matching the exact JSON keys of the BUSINESS-RULES.md §5.3 prompt schema
/// (<c>camelCase</c>, unified label fields split by source field name). Kept separate from the
/// Domain/<see cref="Icbank.Platform.Application.MediaMonitoring.FinalReportSections"/> CLR types
/// so the AI wire contract can evolve independently of the persisted/validated shape. The nested
/// per-section shapes live in their own <c>*Dto.cs</c> files alongside this one (StyleCop SA1402).
/// </summary>
public sealed class FinalReportSectionsJsonDto
{
    /// <summary>Gets or sets the executive summary.</summary>
    [JsonPropertyName("executiveSummary")]
    public string? ExecutiveSummary { get; set; }

    /// <summary>Gets or sets the KPIs.</summary>
    [JsonPropertyName("kpis")]
    public KpisDto? Kpis { get; set; }

    /// <summary>Gets or sets the top-news items.</summary>
    [JsonPropertyName("topNews")]
    public List<TopNewsDto>? TopNews { get; set; }

    /// <summary>Gets or sets the timeline events.</summary>
    [JsonPropertyName("timeline")]
    public List<TimelineDto>? Timeline { get; set; }

    /// <summary>Gets or sets the digital-presence section.</summary>
    [JsonPropertyName("digitalPresence")]
    public DigitalPresenceDto? DigitalPresence { get; set; }

    /// <summary>Gets or sets the editorial-tone section.</summary>
    [JsonPropertyName("editorialTone")]
    public EditorialToneDto? EditorialTone { get; set; }

    /// <summary>Gets or sets the deep-analysis section.</summary>
    [JsonPropertyName("deepAnalysis")]
    public DeepAnalysisDto? DeepAnalysis { get; set; }

    /// <summary>Gets or sets the regional-comparison rows.</summary>
    [JsonPropertyName("regionalComparison")]
    public List<RegionalComparisonDto>? RegionalComparison { get; set; }

    /// <summary>Gets or sets the recommendations.</summary>
    [JsonPropertyName("recommendations")]
    public List<RecommendationDto>? Recommendations { get; set; }

    /// <summary>Gets or sets the alerts.</summary>
    [JsonPropertyName("alerts")]
    public List<AlertDto>? Alerts { get; set; }

    /// <summary>Gets or sets the quotes appendix.</summary>
    [JsonPropertyName("quotesAppendix")]
    public List<QuoteAppendixDto>? QuotesAppendix { get; set; }

    /// <summary>Gets or sets the methodology notes.</summary>
    [JsonPropertyName("methodology")]
    public string? Methodology { get; set; }

    /// <summary>Gets or sets the source list.</summary>
    [JsonPropertyName("sources")]
    public List<SourceRefDto>? Sources { get; set; }
}
