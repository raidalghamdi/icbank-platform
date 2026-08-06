using Icbank.Platform.Application.Weekend;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.Weekend;

/// <summary>
/// Gemini-backed <see cref="IWeekendContentGenerator"/>. Builds the verbatim weekend-draft prompt
/// (BUSINESS-RULES.md §2.3) and calls <see cref="IGeminiClient.GenerateJsonAsync"/>, which applies
/// the Node source's <c>aiJSONWithFallback</c>-equivalent JSON robustness ladder on top of the
/// model fallback chain. The Node source used the "pro" model tier for this call
/// (<c>aiJSONWithFallback</c> defaults to <c>GEMINI_PRO_MODEL</c> for weekend drafts).
/// </summary>
public sealed class GeminiWeekendContentGenerator : IWeekendContentGenerator
{
    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiWeekendContentGenerator"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiWeekendContentGenerator(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(string weekendDate, CancellationToken cancellationToken)
    {
        var prompt = WeekendGenerationPromptTemplate.Build(weekendDate);
        var callOptions = new GeminiCallOptions(_options.ProModel);
        GeminiGenerationResult result = await _client.GenerateJsonAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);
        return result.Text;
    }
}
