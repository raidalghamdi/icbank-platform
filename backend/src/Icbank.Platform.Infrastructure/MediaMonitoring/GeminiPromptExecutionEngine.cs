using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Gemini-backed <see cref="IPromptExecutionEngine"/>. A pure passthrough: the caller (the "AI
/// Quick" 7 fixed tools via <see cref="QuickAiToolPromptTemplates"/>, and <c>POST /prompts/:id/run</c>)
/// has already built the complete prompt text, so this adapter's only job is to route it through
/// the shared Gemini resilience pipeline with no further prompt construction.
/// </summary>
public sealed class GeminiPromptExecutionEngine : IPromptExecutionEngine
{
    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiPromptExecutionEngine"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiPromptExecutionEngine(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<string> ExecuteAsync(string promptText, CancellationToken cancellationToken)
    {
        var callOptions = new GeminiCallOptions(_options.TextModel);
        GeminiGenerationResult result = await _client.GenerateTextAsync(promptText, callOptions, cancellationToken).ConfigureAwait(false);
        return result.Text;
    }
}
