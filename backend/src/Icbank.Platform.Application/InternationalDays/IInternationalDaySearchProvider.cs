namespace Icbank.Platform.Application.InternationalDays;

/// <summary>
/// Port for the AI-backed international-day research search (BUSINESS-RULES.md §4.2's verbatim
/// prompt). The Node source called Perplexity <c>sonar-pro</c> primary with a Gemini-backed
/// Anthropic-shaped adapter fallback, plus a dead "dual-provider merge" path
/// (<c>secondaryResult</c> was always <c>{}</c> -- DEFECT-LOG.md ARCH-07/AMBIGUOUS-BR-6). Per the
/// task's explicit instruction, that dead merge logic is deliberately NOT ported -- this port
/// exposes a single-result contract matching what the live code path actually did. Following the
/// WAVE1-PORT-NOTES.md pattern (<c>IWeekendContentGenerator</c> et al.), the real Perplexity/Gemini
/// HTTP calls are deferred; a deterministic, schema-correct placeholder implementation is
/// registered by default so every downstream endpoint (save/archive/export) is fully exercisable.
/// </summary>
public interface IInternationalDaySearchProvider
{
    /// <summary>Searches for the given day name and returns the structured research result.</summary>
    /// <param name="dayName">The day name to research, e.g. "اليوم العالمي للغة العربية".</param>
    /// <param name="year">The current year, substituted into the prompt template.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The AI-provider's parsed, not-yet-validated search result.</returns>
    Task<DaySearchResultDto> SearchAsync(string dayName, int year, CancellationToken cancellationToken);
}
