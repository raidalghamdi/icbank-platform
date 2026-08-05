namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Port for regenerating just a final report's executive summary (BUSINESS-RULES.md §5.4's
/// verbatim prompt). The Node source called Gemini directly; this port keeps the model call out
/// of Application (R-BE-002) and swappable in Infrastructure. Wave 3a ships a deterministic
/// placeholder implementation -- wiring a real LLM call is deferred, see WAVE3A-PORT-NOTES.md.
/// </summary>
public interface IExecutiveSummaryRegenerator
{
    /// <summary>Regenerates the executive summary for the given report context.</summary>
    /// <param name="title">The report title.</param>
    /// <param name="periodLabel">The report's period label.</param>
    /// <param name="kpisJson">The report's KPIs, pre-serialized to JSON.</param>
    /// <param name="topNewsJson">The report's top-5 news items, pre-serialized to JSON.</param>
    /// <param name="recommendationsJson">The report's top-3 recommendations, pre-serialized to JSON.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The regenerated Arabic executive-summary Markdown text.</returns>
    Task<string> RegenerateAsync(
        string title, string periodLabel, string kpisJson, string topNewsJson, string recommendationsJson, CancellationToken cancellationToken);
}
