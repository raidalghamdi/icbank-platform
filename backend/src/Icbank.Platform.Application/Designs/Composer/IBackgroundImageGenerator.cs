namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>
/// Port for the AI background-image generation call (ports <c>gemini-2.5-flash-image</c> aka
/// "Nano Banana", BUSINESS-RULES.md §7.3). Following the established narrow-named-interface +
/// deterministic-placeholder pattern, the real Gemini image-generation call is deferred; the
/// default implementation returns a schema-correct placeholder so the endpoint (spatial-hint
/// prompt assembly, rate limiting, partial-success aggregation, audit logging, storage
/// persistence) is fully exercisable end-to-end.
/// </summary>
public interface IBackgroundImageGenerator
{
    /// <summary>Generates one background-image variant for the given fully-assembled prompt.</summary>
    /// <param name="prompt">The fully-assembled image prompt, including the spatial-awareness hint.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated image bytes and MIME content type.</returns>
    Task<GeneratedBackgroundImage> GenerateAsync(string prompt, CancellationToken cancellationToken);
}
