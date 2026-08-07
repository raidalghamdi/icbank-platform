namespace Icbank.Platform.Application.Designs.IconEvent;

/// <summary>
/// Port for the AI-backed icon-event extraction call (BUSINESS-RULES.md §7.4's verbatim prompt,
/// <see cref="Icbank.Platform.Domain.Designs.IconEventExtractionPrompts"/>). The Node source used
/// a Gemini-chain-then-Perplexity fallback (<c>aiJSONWithFallback</c>). Following the
/// WAVE1/WAVE2-PORT-NOTES.md narrow-named-interface pattern, the real provider call is deferred;
/// a deterministic placeholder is registered by default so the endpoint is fully exercisable
/// end-to-end. The prompt text itself is assembled by <see cref="IconEventPromptBuilder"/> and
/// passed in so a real implementation never has to know the prompt's internal structure.
/// </summary>
public interface IIconEventDesignExtractor
{
    /// <summary>Calls the AI provider with the fully-assembled prompt and returns its typed, not-yet-validated response.</summary>
    /// <param name="prompt">The fully-assembled extraction prompt.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The typed extraction result.</returns>
    Task<IconEventExtractionResultDto> ExtractAsync(string prompt, CancellationToken cancellationToken);
}
