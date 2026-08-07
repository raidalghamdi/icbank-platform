namespace Icbank.Platform.Application.Weekend;

/// <summary>
/// Port for generating the weekend content bundle (BUSINESS-RULES.md §2.3's verbatim prompt: 4
/// places, 3 deal categories × 3 items, 3 podcasts, 3 matches, 3 movies, all Riyadh-only). The
/// Node source used a multi-provider Gemini→Perplexity fallback chain
/// (<c>aiJSONWithFallback</c>); this port keeps the model call out of Application (R-BE-002).
/// Wave 1 ships a deterministic, non-AI placeholder implementation — wiring a real LLM provider
/// chain is deferred, see WAVE1-PORT-NOTES.md.
/// </summary>
public interface IWeekendContentGenerator
{
    /// <summary>Generates the weekend content bundle JSON for the given target weekend date.</summary>
    /// <param name="weekendDate">The ISO (<c>yyyy-MM-dd</c>) target Thursday.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated content payload as JSON text, matching the <c>{summary,places[],deals[],podcasts[],matches[],movies[]}</c> shape.</returns>
    Task<string> GenerateAsync(string weekendDate, CancellationToken cancellationToken);
}
