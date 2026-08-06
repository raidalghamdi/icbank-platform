using System.Text;
using Icbank.Platform.Application.Dashboard;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.Dashboard;

/// <summary>
/// Gemini-backed <see cref="IExecutiveSummaryGenerator"/>. Wraps the caller's pre-built data
/// digest in the verbatim single-shot prompt from BUSINESS-RULES.md §9 (<c>dashboard.ts:168</c>)
/// and calls the shared <see cref="IGeminiClient"/> resilience pipeline (model fallback chain +
/// 2-attempts-per-model retry, ported from <c>aiProviders.ts</c>'s <c>geminiText</c>).
/// </summary>
public sealed class GeminiExecutiveSummaryGenerator : IExecutiveSummaryGenerator
{
    // Why: verbatim from dashboard.ts:168 (BUSINESS-RULES.md §9) — not paraphrased.
    private const string RawTemplate =
        "أنت مساعد تنفيذي متخصص في التواصل الداخلي المؤسسي. بناءً على البيانات التالية:\n{0}\n\n" +
        "اكتب ملخصاً تنفيذياً قصيراً (3-4 نقاط عربية) يلخص نشاط الإدارة في التواصل الداخلي. كل نقطة في سطر منفصل تبدأ بـ •. كن موجزاً ومهنياً.";

    private static readonly CompositeFormat PromptTemplate = CompositeFormat.Parse(RawTemplate);

    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiExecutiveSummaryGenerator"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiExecutiveSummaryGenerator(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<string> GenerateAsync(string dataDigest, CancellationToken cancellationToken)
    {
        var prompt = string.Format(System.Globalization.CultureInfo.InvariantCulture, PromptTemplate, dataDigest);
        var callOptions = new GeminiCallOptions(_options.TextModel);
        GeminiGenerationResult result = await _client.GenerateTextAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);
        return result.Text;
    }
}
