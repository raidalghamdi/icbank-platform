using Icbank.Platform.Domain.MediaMonitoring;

namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Port for generating the canonical 8-section GAC final-report JSON (BUSINESS-RULES.md §5.3's
/// verbatim prompt). The Node source used a Gemini→Perplexity fallback chain
/// (<c>aiJSONWithFallback</c>); this port keeps the model call out of Application (R-BE-002) and
/// swappable in Infrastructure. Wave 3a ships a deterministic, schema-correct placeholder
/// implementation -- wiring a real LLM provider chain is deferred, see WAVE3A-PORT-NOTES.md.
/// Closes DEFECT-LOG.md DATA-04/H-2: the generated sections are typed CLR objects, never raw
/// AI JSON text, so they pass through <see cref="FluentValidation"/> before persistence exactly
/// like any other command input.
/// </summary>
public interface IFinalReportSectionGenerator
{
    /// <summary>Generates the 8-section report content for the given period/audience/focus topics and source feed.</summary>
    /// <param name="periodLabel">The human-readable period label.</param>
    /// <param name="audience">The target audience description.</param>
    /// <param name="focusTopics">The optional focus-topics free text.</param>
    /// <param name="formattedFeed">The flat numbered text block of source posts/news items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated 8-section report content.</returns>
    Task<FinalReportSections> GenerateAsync(
        string periodLabel, string audience, string? focusTopics, string formattedFeed, CancellationToken cancellationToken);
}

/// <summary>The typed 8-section content bundle produced by <see cref="IFinalReportSectionGenerator"/> (BUSINESS-RULES.md §5.3).</summary>
public sealed class FinalReportSections
{
    /// <summary>Gets or sets the executive summary (section 1).</summary>
    public string ExecutiveSummary { get; set; } = string.Empty;

    /// <summary>Gets or sets the report's key performance indicators.</summary>
    public ReportKpis Kpis { get; set; } = new();

    /// <summary>Gets or sets the top news items (section 2).</summary>
    public List<TopNewsItem> TopNews { get; set; } = new();

    /// <summary>Gets or sets the detailed timeline (section 3).</summary>
    public List<TimelineEvent> Timeline { get; set; } = new();

    /// <summary>Gets or sets the digital-presence analysis (section 4).</summary>
    public DigitalPresence DigitalPresence { get; set; } = new();

    /// <summary>Gets or sets the editorial-tone analysis (section 5).</summary>
    public EditorialTone EditorialTone { get; set; } = new();

    /// <summary>Gets or sets the deep sectoral analysis (section 6).</summary>
    public DeepAnalysis DeepAnalysis { get; set; } = new();

    /// <summary>Gets or sets the regional comparison table (section 7).</summary>
    public List<RegionalComparison> RegionalComparison { get; set; } = new();

    /// <summary>Gets or sets the recommendations and action plan (section 8a).</summary>
    public List<Recommendation> Recommendations { get; set; } = new();

    /// <summary>Gets or sets the alerts and suggested positions (section 8b).</summary>
    public List<AlertItem> Alerts { get; set; } = new();

    /// <summary>Gets or sets the quotes appendix.</summary>
    public List<QuoteAppendixItem> QuotesAppendix { get; set; } = new();

    /// <summary>Gets or sets the methodology notes.</summary>
    public string Methodology { get; set; } = string.Empty;

    /// <summary>Gets or sets the source list.</summary>
    public List<SourceRef> Sources { get; set; } = new();
}
