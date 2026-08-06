using System.Globalization;
using System.Text;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Infrastructure.Gemini;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Gemini-backed <see cref="IReportArchiveQaEngine"/>. Builds the verbatim dual-mode Q&amp;A
/// prompt from BUSINESS-RULES.md §5.5 (<c>final-media-reports.ts:704-711</c>), info mode only --
/// full mode never reaches this adapter (it returns matched report rows directly, no AI call).
/// </summary>
public sealed class GeminiReportArchiveQaEngine : IReportArchiveQaEngine
{
    private const string RawTemplate = """
أنت مساعد بحث ذكي في أرشيف تقارير الرصد الإعلامي للهيئة العامة للمنافسة.

السؤال: {0}

السياق من التقارير المحفوظة:
{1}

أجب بدقة بناءً على السياق فقط، باللغة العربية الرسمية. أضف في النهاية قائمة "المصادر:" بأرقام التقارير التي اعتمدت عليها. إذا لم يكن السياق كافياً، صرّح بذلك.
""";

    private static readonly CompositeFormat Template = CompositeFormat.Parse(RawTemplate);

    private readonly IGeminiClient _client;
    private readonly GeminiOptions _options;

    /// <summary>Initializes a new instance of the <see cref="GeminiReportArchiveQaEngine"/> class.</summary>
    /// <param name="client">The resilience-aware Gemini client.</param>
    /// <param name="options">The Gemini model configuration.</param>
    public GeminiReportArchiveQaEngine(IGeminiClient client, GeminiOptions options)
    {
        _client = client;
        _options = options;
    }

    /// <inheritdoc />
    public async Task<string> AnswerAsync(string query, string context, CancellationToken cancellationToken)
    {
        var prompt = string.Format(CultureInfo.InvariantCulture, Template, query, context);
        var callOptions = new GeminiCallOptions(_options.TextModel);
        GeminiGenerationResult result = await _client.GenerateTextAsync(prompt, callOptions, cancellationToken).ConfigureAwait(false);
        return result.Text;
    }
}
