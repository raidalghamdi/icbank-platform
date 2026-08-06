namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// The resilience-aware Gemini entry point every adapter calls through. Owns the model-fallback
/// chain, the 2-attempts-per-model retry/backoff loop, and (for <see cref="GenerateJsonAsync"/>)
/// the JSON-parse-with-repair ladder — all ported verbatim from <c>aiProviders.ts</c>'s
/// <c>geminiText</c>/<c>geminiJSON</c>.
/// </summary>
public interface IGeminiClient
{
    /// <summary>Generates plain text, retrying/falling back per the ported Node resilience policy.</summary>
    /// <param name="prompt">The user prompt text.</param>
    /// <param name="options">Model/tuning options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated result, including grounding metadata when requested.</returns>
    Task<GeminiGenerationResult> GenerateTextAsync(string prompt, GeminiCallOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Generates JSON, prepending the Node source's verbatim Arabic JSON-only system prefix and
    /// applying its 3-attempt parse-with-repair ladder (strip ```` ```json ```` fences, try direct
    /// parse, then trim to the last complete <c>}</c>/<c>]</c> and retry) on top of the normal
    /// model retry/fallback loop.
    /// </summary>
    /// <param name="prompt">The user prompt text (the caller's schema instructions).</param>
    /// <param name="options">Model/tuning options.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated result, with <see cref="GeminiGenerationResult.Text"/> guaranteed to be valid, parseable JSON text.</returns>
    Task<GeminiGenerationResult> GenerateJsonAsync(string prompt, GeminiCallOptions options, CancellationToken cancellationToken);

    /// <summary>
    /// Generates an image via Gemini's image-generation model (<c>gemini-2.5-flash-image</c>, aka
    /// "Nano Banana"). Applies the same 2-attempts-per-model retry/backoff as text/JSON calls, but
    /// against a single-model "chain" -- the Node original documented no image-model fallback
    /// tier, only the text-model fallback chain.
    /// </summary>
    /// <param name="prompt">The fully-assembled image prompt.</param>
    /// <param name="options">Model/tuning options (<see cref="GeminiCallOptions.Model"/> should be the image model).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The generated result, with <see cref="GeminiGenerationResult.InlineImages"/> guaranteed non-empty.</returns>
    Task<GeminiGenerationResult> GenerateImageAsync(string prompt, GeminiCallOptions options, CancellationToken cancellationToken);
}
