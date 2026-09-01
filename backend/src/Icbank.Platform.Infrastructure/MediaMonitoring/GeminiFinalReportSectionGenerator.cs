using System.Text.Json;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Gemini-backed <see cref="IFinalReportSectionGenerator"/>. Builds the verbatim canonical
/// 8-section prompt (BUSINESS-RULES.md §5.3), calls <see cref="IGeminiClient.GenerateJsonAsync"/>
/// (pro model tier -- this is the highest-stakes, longest-output call in the system), and maps
/// the parsed wire JSON onto the persisted <see cref="FinalReportSections"/> shape via
/// <see cref="FinalReportSectionsMapper"/>. The zero-source-item NO_SOURCE_DATA 422 short-circuit
/// lives in <c>GenerateFinalMediaReportCommandHandler</c> -- this adapter is only ever invoked
/// once there is source data.
/// </summary>
public sealed class GeminiFinalReportSectionGenerator : IFinalReportSectionGenerator
{
    /// <summary>The reasoning-token cap applied to the report call. See the comment at the call site for the measurements behind the number.</summary>
    private const int ReportThinkingBudget = 512;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiFinalReportSectionGenerator"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiFinalReportSectionGenerator(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<FinalReportSections> GenerateAsync(
        string periodLabel, string audience, string? focusTopics, string formattedFeed, CancellationToken cancellationToken)
    {
        var prompt = FinalReportPromptTemplate.Build(periodLabel, audience, focusTopics, formattedFeed);

        // Measured against the live prompt on 2026-09-01: gemini-2.5-pro with its dynamic
        // reasoning budget took 66 s and the endpoint 75-85 s end to end, which reads as a hung
        // button. Capping the budget cut the same call to 35 s with all thirteen sections intact,
        // and it fixed a worse failure on the fallback leg: uncapped gemini-2.5-flash spent 7,403
        // of the 8,192 allowed tokens thinking, returned truncated JSON, failed to parse, and sent
        // GenerateJsonAsync round the whole four-model chain again -- minutes of waiting ending in
        // an error. Capped, that same fallback answers in 18 s and parses.
        var callOptions = new GeminiCallOptions(_options.ProModel, MaxOutputTokens: 8192, ThinkingBudget: ReportThinkingBudget);
        GeminiGenerationResult result = await _client.GenerateJsonAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);

        FinalReportSectionsJsonDto dto = JsonSerializer.Deserialize<FinalReportSectionsJsonDto>(result.Text, JsonOptions)
            ?? throw new GeminiUnavailableException("Gemini returned an empty/null JSON payload for the final report sections.");

        return FinalReportSectionsMapper.Map(dto);
    }
}
