namespace Icbank.Platform.Application.MediaMonitoring;

/// <summary>
/// Port for answering a free-text question against a set of matched final-report excerpts
/// (BUSINESS-RULES.md §5.5's verbatim dual-mode Q&amp;A prompt, info mode only -- full mode never
/// calls AI). The Node source called Gemini directly; this port keeps the model call out of
/// Application (R-BE-002) and swappable in Infrastructure. Wave 3a ships a deterministic
/// placeholder implementation -- wiring a real LLM call is deferred, see WAVE3A-PORT-NOTES.md.
/// </summary>
public interface IReportArchiveQaEngine
{
    /// <summary>Answers a question against the given report-excerpt context.</summary>
    /// <param name="query">The caller's free-text question.</param>
    /// <param name="context">The concatenated report excerpts (executiveSummary/topNews/recommendations/deepAnalysis/quotesAppendix).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The model's Arabic answer text, including a trailing "المصادر:" source list.</returns>
    Task<string> AnswerAsync(string query, string context, CancellationToken cancellationToken);
}
