using Icbank.Platform.Application.Shorfah;

namespace Icbank.Platform.Infrastructure.Shorfah;

/// <summary>
/// Deterministic, non-LLM-backed default <see cref="IShorfahSectionContentGenerator"/>
/// implementation. The Node source called Gemini; wiring a real provider is deferred (see
/// WAVE4B-PORT-NOTES.md), following the exact same deferral pattern as
/// <c>TemplateMediaReportNarrativeGenerator</c> (wave 3a) and
/// <c>TemplateInternationalDaySearchProvider</c> (wave 2). The output is always
/// schema-correct, clearly labeled Arabic content -- never a fabricated success.
/// </summary>
public sealed class TemplateShorfahSectionContentGenerator : IShorfahSectionContentGenerator
{
    /// <inheritdoc />
    public Task<ShorfahGeneratedSectionContent> GenerateAsync(string prompt, CancellationToken cancellationToken)
    {
        var contentMd = "## محتوى مؤقت بانتظار ربط مزوّد الذكاء الاصطناعي\n\n" +
            "تم إنشاء هذا المحتوى بواسطة المولّد الاحتياطي المحلي بدلاً من مزوّد ذكاء اصطناعي حقيقي.\n\n" +
            $"طول الطلب المُرسل: {prompt.Length} حرفاً.";
        return Task.FromResult(new ShorfahGeneratedSectionContent(contentMd));
    }
}
