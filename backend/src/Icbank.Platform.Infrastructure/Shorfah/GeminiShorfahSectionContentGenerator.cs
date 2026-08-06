using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.Shorfah;

/// <summary>
/// Gemini-backed <see cref="IShorfahSectionContentGenerator"/>. The prompt is fully assembled by
/// <see cref="ShorfahGenerationPrompts.BuildPrompt"/> in the Application layer (BUSINESS-RULES.md
/// §1.8, verbatim); this adapter's only job is to call <c>geminiJSON</c>'s ported equivalent
/// (<see cref="IGeminiClient.GenerateJsonAsync"/>, matching the Node source's 2000 max-output-token
/// cap for this call, <c>shorfah.ts:471-513</c>) and map the single <c>content_md</c> field.
/// </summary>
public sealed class GeminiShorfahSectionContentGenerator : IShorfahSectionContentGenerator
{
    private const int MaxOutputTokens = 2000;

    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiShorfahSectionContentGenerator"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiShorfahSectionContentGenerator(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<ShorfahGeneratedSectionContent> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        var callOptions = new GeminiCallOptions(_options.TextModel, MaxOutputTokens: MaxOutputTokens);
        GeminiGenerationResult result = await _client.GenerateJsonAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);

        using var parsed = System.Text.Json.JsonDocument.Parse(result.Text);
        var contentMd = parsed.RootElement.TryGetProperty("content_md", out var contentProp) ? contentProp.GetString() ?? string.Empty : result.Text;
        return new ShorfahGeneratedSectionContent(contentMd);
    }
}
