using Icbank.Platform.Application.MediaMonitoring;

namespace Icbank.Platform.Infrastructure.MediaMonitoring;

/// <summary>
/// Deterministic, non-AI default <see cref="IMediaReportNarrativeGenerator"/> implementation.
/// The Node source made three separate Gemini calls with the audience-tiered prompts in
/// BUSINESS-RULES.md §5.1; wiring a real LLM provider chain is deferred for Wave 3a (see
/// WAVE3A-PORT-NOTES.md) -- this implementation formats the source feed into a clearly-labeled
/// placeholder Markdown body so every downstream endpoint (list/get/delete) is fully exercisable
/// end-to-end without an external AI dependency.
/// </summary>
public sealed class TemplateMediaReportNarrativeGenerator : IMediaReportNarrativeGenerator
{
    private const int FeedPreviewLineCount = 5;
    private const string ExecutiveSummary = "ملخص تنفيذي مؤقت بانتظار ربط مزوّد الذكاء الاصطناعي.";
    private const string OverallTone = "محايد عام";

    /// <inheritdoc />
    public Task<MediaReportNarrative> GenerateAsync(string audience, string formattedFeed, CancellationToken cancellationToken)
    {
        var lines = formattedFeed.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var preview = string.Join('\n', lines.Take(FeedPreviewLineCount));

        var contentMd = "## ملخص الفترة (نموذج مؤقت بانتظار ربط مزوّد الذكاء الاصطناعي)\n\n" +
            $"الجمهور المستهدف: {audience}\n\n" +
            $"عدد العناصر المرصودة: {lines.Length}\n\n" +
            "## أبرز العناصر\n" + preview;

        return Task.FromResult(new MediaReportNarrative(contentMd, ExecutiveSummary, OverallTone));
    }
}
