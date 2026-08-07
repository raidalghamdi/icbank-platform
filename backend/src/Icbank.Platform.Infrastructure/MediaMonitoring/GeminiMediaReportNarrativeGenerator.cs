using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Gemini-backed <see cref="IMediaReportNarrativeGenerator"/>. Ports BUSINESS-RULES.md §5.1's
/// 3-call pipeline verbatim: (1) the audience-tiered Markdown body over the full feed, (2) a
/// separate 2-3 line executive-summary call capped at 300 max output tokens, (3) a separate
/// two-word overall-tone classification call capped at 50 max output tokens. The zero-source-item
/// "no AI call" short-circuit lives in <c>GenerateMediaReportCommandHandler</c> (Application
/// layer) — this adapter is only ever invoked once there is at least one post or news item.
/// </summary>
public sealed class GeminiMediaReportNarrativeGenerator : IMediaReportNarrativeGenerator
{
    private const int ExecutiveSummaryMaxTokens = 300;
    private const int ToneMaxTokens = 50;

    private const string ExecutiveSummaryPromptPrefix =
        "لخّص الفترة التالية في 2-3 أسطر تنفيذية موجزة بالعربية الفصحى، دون عناوين أو تنسيق Markdown:\n\n";

    private const string TonePromptPrefix =
        "صف النبرة العامة للمحتوى التالي بكلمتين فقط بالعربية (مثل: إيجابي عام، محايد متوازن):\n\n";

    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiMediaReportNarrativeGenerator"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiMediaReportNarrativeGenerator(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<MediaReportNarrative> GenerateAsync(string audience, string formattedFeed, CancellationToken cancellationToken)
    {
        Domain.MediaMonitoring.MediaReportAudience parsedAudience =
            Enum.TryParse(audience, ignoreCase: true, out Domain.MediaMonitoring.MediaReportAudience value) ? value : Domain.MediaMonitoring.MediaReportAudience.Manager;
        var audiencePrompt = AudienceReportPromptTemplates.Resolve(parsedAudience);

        var bodyPrompt = audiencePrompt + "\n\nالبيانات المرصودة:\n" + formattedFeed;
        var contentMd = await CallTextAsync(bodyPrompt, GeminiClient.DefaultMaxOutputTokens, cancellationToken).ConfigureAwait(false);

        var summaryPrompt = ExecutiveSummaryPromptPrefix + formattedFeed;
        var executiveSummary = await CallTextAsync(summaryPrompt, ExecutiveSummaryMaxTokens, cancellationToken).ConfigureAwait(false);

        var tonePrompt = TonePromptPrefix + formattedFeed;
        var overallTone = await CallTextAsync(tonePrompt, ToneMaxTokens, cancellationToken).ConfigureAwait(false);

        return new MediaReportNarrative(contentMd, executiveSummary, overallTone);
    }

    private async Task<string> CallTextAsync(string prompt, int maxOutputTokens, CancellationToken cancellationToken)
    {
        var callOptions = new GeminiCallOptions(_options.TextModel, MaxOutputTokens: maxOutputTokens);
        GeminiGenerationResult result = await _client.GenerateTextAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);
        return result.Text;
    }
}
