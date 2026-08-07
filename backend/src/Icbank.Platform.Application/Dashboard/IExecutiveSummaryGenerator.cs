namespace Icbank.Platform.Application.Dashboard;

/// <summary>
/// Port for generating the Arabic executive-summary text (BUSINESS-RULES.md §9's verbatim
/// prompt). The Node source called Gemini via an Anthropic-shaped adapter; this port keeps the
/// actual model call out of Application (R-BE-002) and swappable in Infrastructure. Wave 1 ships
/// a deterministic, non-AI default implementation (<c>TemplateExecutiveSummaryGenerator</c>) —
/// wiring a real LLM call is deferred, see WAVE1-PORT-NOTES.md.
/// </summary>
public interface IExecutiveSummaryGenerator
{
    /// <summary>Generates the executive-summary text from a pre-formatted data digest.</summary>
    /// <param name="dataDigest">The 3-line data digest (activation count, recent activations, recent week-start titles).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated Arabic summary text.</returns>
    Task<string> GenerateAsync(string dataDigest, CancellationToken cancellationToken);
}
