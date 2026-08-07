using Icbank.Platform.Application.MediaMonitoring;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Deterministic, non-AI default <see cref="IExecutiveSummaryRegenerator"/> implementation. The
/// Node source called Gemini directly with the verbatim prompt in BUSINESS-RULES.md §5.4; wiring
/// a real LLM call is deferred for Wave 3a (see WAVE3A-PORT-NOTES.md) -- this implementation
/// produces a clearly-labeled placeholder Markdown summary derived from the report's own title
/// and period, so the exec-summary endpoint is fully exercisable end-to-end without an external
/// AI dependency.
/// </summary>
public sealed class TemplateExecutiveSummaryRegenerator : IExecutiveSummaryRegenerator
{
    /// <inheritdoc />
    public Task<string> RegenerateAsync(
        string title, string periodLabel, string kpisJson, string topNewsJson, string recommendationsJson, CancellationToken cancellationToken)
    {
        var summary = "## ملخص تنفيذي مؤقت (بانتظار ربط مزوّد الذكاء الاصطناعي)\n\n" +
            $"التقرير: {title}\n\n" +
            $"الفترة: {periodLabel}\n\n" +
            "تم إعادة توليد هذا الملخص من نموذج مؤقت لا يعتمد على استدعاء خارجي، بناءً على مؤشرات الأداء وأبرز الأخبار والتوصيات المسجلة في التقرير.";

        return Task.FromResult(summary);
    }
}
