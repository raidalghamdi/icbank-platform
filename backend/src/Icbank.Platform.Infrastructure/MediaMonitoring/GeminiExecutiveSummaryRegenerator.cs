using System.Globalization;
using System.Text;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Gemini-backed <see cref="IExecutiveSummaryRegenerator"/>. Builds the verbatim regeneration
/// prompt from BUSINESS-RULES.md §5.4 (<c>final-media-reports.ts:626-637</c>) around the caller's
/// pre-serialized/pre-sliced (top-5 news, top-3 recommendations) JSON fragments.
/// </summary>
public sealed class GeminiExecutiveSummaryRegenerator : IExecutiveSummaryRegenerator
{
    private const string RawTemplate = """
أنت محلل تنفيذي. ولّد ملخصاً تنفيذياً موجزاً (5-7 أسطر فقط) للقيادة العليا بصيغة Markdown عربية، اعتماداً على التقرير التالي:

العنوان: {0}
الفترة: {1}
المؤشرات: {2}
أبرز الأخبار: {3}
التوصيات: {4}

أنتج:
## الملخص التنفيذي — {1}

ثم 3-4 نقاط مرقمة + توصية تنفيذية واحدة في النهاية. لغة موجزة احترافية.
""";

    private static readonly CompositeFormat Template = CompositeFormat.Parse(RawTemplate);

    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiExecutiveSummaryRegenerator"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiExecutiveSummaryRegenerator(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<string> RegenerateAsync(
        string title, string periodLabel, string kpisJson, string topNewsJson, string recommendationsJson, CancellationToken cancellationToken)
    {
        var prompt = string.Format(CultureInfo.InvariantCulture, Template, title, periodLabel, kpisJson, topNewsJson, recommendationsJson);
        var callOptions = new GeminiCallOptions(_options.TextModel);
        GeminiGenerationResult result = await _client.GenerateTextAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);
        return result.Text;
    }
}
