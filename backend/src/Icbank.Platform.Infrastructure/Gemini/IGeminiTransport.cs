namespace Icbank.Platform.Infrastructure.Gemini;

/// <summary>
/// Low-level, single-attempt HTTP transport to the Gemini REST API. Deliberately narrow (one
/// method, one model, no retry) so <see cref="GeminiClient"/> can own all retry/fallback/backoff
/// policy while tests fake this seam directly instead of mocking <see cref="HttpMessageHandler"/>.
/// </summary>
public interface IGeminiTransport
{
    /// <summary>Issues exactly one <c>generateContent</c> HTTP call. Throws on any non-success HTTP status or transport failure; never retries.</summary>
    /// <param name="apiKey">The resolved Gemini API key.</param>
    /// <param name="request">The request to send.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The parsed successful result.</returns>
    Task<GeminiGenerationResult> GenerateContentAsync(string apiKey, GeminiGenerationRequest request, CancellationToken cancellationToken);
}
