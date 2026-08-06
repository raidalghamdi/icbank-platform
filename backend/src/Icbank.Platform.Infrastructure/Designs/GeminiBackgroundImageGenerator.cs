using Icbank.Platform.Application.Designs.Composer;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>
/// Gemini-backed <see cref="IBackgroundImageGenerator"/>. Calls Gemini's image-generation model
/// (<c>gemini-2.5-flash-image</c>, aka "Nano Banana") via <see cref="IGeminiClient.GenerateImageAsync"/>
/// with the fully-assembled prompt from <see cref="BackgroundPromptBuilder"/> (Application layer,
/// BUSINESS-RULES.md §7.3, verbatim spatial-hint + quality suffix). Decodes the first inline
/// image part into raw bytes -- this is the only adapter in the port whose output is binary
/// rather than text/JSON.
/// </summary>
public sealed class GeminiBackgroundImageGenerator : IBackgroundImageGenerator
{
    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiBackgroundImageGenerator"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiBackgroundImageGenerator(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<GeneratedBackgroundImage> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        var callOptions = new GeminiCallOptions(_options.ImageModel);
        GeminiGenerationResult result = await _client.GenerateImageAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);

        GeminiInlineImage image = result.InlineImages[0];
        var bytes = Convert.FromBase64String(image.Base64Data);
        return new GeneratedBackgroundImage(bytes, image.MimeType);
    }
}
