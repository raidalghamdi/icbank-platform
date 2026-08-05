using Icbank.Platform.Application.MediaMonitoring;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Deterministic, non-AI default <see cref="IReportArchiveQaEngine"/> implementation. The Node
/// source called Gemini directly over the concatenated report-excerpt context built by
/// <see cref="ReportArchiveContextBuilder"/> (BUSINESS-RULES.md §5.5, info mode only); wiring a
/// real LLM call is deferred for Wave 3a (see WAVE3A-PORT-NOTES.md) -- this implementation
/// returns a clearly-labeled placeholder answer that echoes the caller's question and a preview
/// of the matched context, ending with the same "المصادر:" trailer the Node source's prompt
/// mandated, so the search endpoint's info mode is fully exercisable end-to-end without an
/// external AI dependency.
/// </summary>
public sealed class TemplateReportArchiveQaEngine : IReportArchiveQaEngine
{
    private const int ContextPreviewLength = 400;

    /// <inheritdoc />
    public Task<string> AnswerAsync(string query, string context, CancellationToken cancellationToken)
    {
        var preview = context.Length <= ContextPreviewLength ? context : context[..ContextPreviewLength];
        var answer = "إجابة مؤقتة (بانتظار ربط مزوّد الذكاء الاصطناعي) عن السؤال: " + query + "\n\n" +
            "مقتطف من السياق المطابق:\n" + preview + "\n\nالمصادر: أرشيف التقارير النهائية المطابقة لهذا الاستعلام.";

        return Task.FromResult(answer);
    }
}
