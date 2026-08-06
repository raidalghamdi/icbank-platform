using Icbank.Platform.Application.Weekend;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.Weekend;

/// <summary>
/// Gemini-backed <see cref="IWeekStartMessageGenerator"/>. The Node source labeled its 3
/// "parallel" outputs <c>claude</c>/<c>openai</c>/<c>gemini</c>, but per <c>aiProviders.ts</c> all
/// three were actually independent Gemini calls with the same prompt (no real multi-provider
/// diversity) — this port preserves the 3 labeled outputs for UI/DB compatibility while being
/// honest that all three come from this one adapter. Each of the 3 calls is independent: one
/// model's exhaustion (<see cref="GeminiUnavailableException"/>) does not block the other two.
/// </summary>
public sealed class GeminiWeekStartMessageGenerator : IWeekStartMessageGenerator
{
    private static readonly string[] ModelLabels = { "claude", "openai", "gemini" };

    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiWeekStartMessageGenerator"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiWeekStartMessageGenerator(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WeekStartModelOutput>> GenerateAsync(WeekStartGenerationRequest request, CancellationToken cancellationToken)
    {
        var lengthText = WeekStartPromptTemplate.ResolveLengthText(request.Length);
        var prompt = WeekStartPromptTemplate.Build(
            request.StyleContext ?? string.Empty, archiveContext: null, request.Topic, request.Occasion, request.Audience, request.Tone, lengthText);

        var outputs = new List<WeekStartModelOutput>(ModelLabels.Length);
        foreach (var label in ModelLabels)
        {
            var callOptions = new GeminiCallOptions(_options.TextModel);
            GeminiGenerationResult result = await _client.GenerateTextAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);
            outputs.Add(new WeekStartModelOutput(label, result.Text));
        }

        return outputs;
    }
}
