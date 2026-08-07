using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Domain.MediaMonitoring;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Deterministic, non-AI default <see cref="IFinalReportSectionGenerator"/> implementation. The
/// Node source used a Gemini→Perplexity fallback chain with the verbatim prompt in
/// BUSINESS-RULES.md §5.3; wiring a real LLM provider chain is deferred for Wave 3a (see
/// WAVE3A-PORT-NOTES.md) -- this implementation produces a schema-correct, clearly-labeled
/// placeholder bundle (matching the fixed 4-platform digital-presence list) so every downstream
/// endpoint (list/get/export/email/search) is fully exercisable end-to-end without an external
/// AI dependency.
/// </summary>
public sealed class TemplateFinalReportSectionGenerator : IFinalReportSectionGenerator
{
    private static readonly string[] FixedPlatforms = { "إكس", "لينكدإن", "تليجرام", "يوتيوب" };

    /// <inheritdoc />
    public Task<FinalReportSections> GenerateAsync(
        string periodLabel, string audience, string? focusTopics, string formattedFeed, CancellationToken cancellationToken)
    {
        var lineCount = formattedFeed.Split('\n', StringSplitOptions.RemoveEmptyEntries).Length;
        var sections = new FinalReportSections
        {
            ExecutiveSummary = $"ملخص تنفيذي مؤقت للفترة {periodLabel} (بانتظار ربط مزوّد الذكاء الاصطناعي). عدد العناصر المرصودة: {lineCount}.",
            Kpis = new ReportKpis { TotalNews = lineCount, PositivePercent = 0, MediaOutlets = 0, KeyTopics = 0, Reach = "غير متاح", AlertsCount = 0 },
            DigitalPresence = BuildDigitalPresence(),
            Methodology = "منهجية رصد مؤقتة بانتظار ربط مزوّد الذكاء الاصطناعي.",
        };

        return Task.FromResult(sections);
    }

    private static DigitalPresence BuildDigitalPresence() => new()
    {
        Platforms = FixedPlatforms
            .Select(name => new DigitalPresencePlatform { Name = name, Mentions = 0, Reposts = 0, Engagement = 0, Reach = "غير متاح" })
            .ToList(),
    };
}
