namespace Icbank.Platform.Application.Weekend;

/// <summary>
/// Port for generating the 3 parallel week-start message drafts (BUSINESS-RULES.md §2.5's
/// verbatim prompt). The Node source streamed Claude/GPT-4o/Gemini outputs over SSE (all three
/// actually routed through the same Gemini backend via adapters); this port synchronously
/// returns all three model outputs rather than streaming, since SSE has no established
/// convention in this codebase yet (see WAVE1-PORT-NOTES.md) -- the archive/style-profile context
/// assembly and the resulting persisted GeneratedOutput rows are ported faithfully; only the
/// transport (SSE vs. synchronous JSON) differs.
/// </summary>
public interface IWeekStartMessageGenerator
{
    /// <summary>Generates one message per model for the given topic/style context.</summary>
    /// <param name="request">The generation request parameters.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One generated text per model (<c>claude</c>, <c>openai</c>, <c>gemini</c>).</returns>
    Task<IReadOnlyList<WeekStartModelOutput>> GenerateAsync(WeekStartGenerationRequest request, CancellationToken cancellationToken);
}
