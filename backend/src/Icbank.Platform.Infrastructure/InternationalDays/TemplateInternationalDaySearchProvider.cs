using Icbank.Platform.Application.InternationalDays;

namespace Icbank.Platform.Infrastructure.InternationalDays;

/// <summary>
/// Deterministic, non-AI default <see cref="IInternationalDaySearchProvider"/> implementation.
/// The Node source called Perplexity <c>sonar-pro</c> (primary, 40s timeout) with a Gemini-backed
/// Anthropic-shaped adapter fallback (55s hard wall-clock timeout); wiring a real provider chain
/// is deferred for this wave (see WAVE2-PORT-NOTES.md) -- this implementation produces a
/// schema-correct, clearly-labeled placeholder result satisfying the exact required counts
/// (8-15 activations, 3-5 design samples, >=5 suggestions -- BUSINESS-RULES.md §4.2) so every
/// downstream endpoint (save/archive/export) is fully exercisable end-to-end without an external
/// AI dependency. When a real provider is wired, it must use the shared "downstream"
/// <c>HttpClient</c> (Polly retry/backoff/timeout already configured -- see
/// Infrastructure.DependencyInjection.AddResilientHttpClients) and must never log the API key.
/// </summary>
public sealed class TemplateInternationalDaySearchProvider : IInternationalDaySearchProvider
{
    private const int ActivationCount = 8;
    private const int DesignSampleCount = 3;
    private const int SuggestionCount = 5;

    /// <inheritdoc />
    public Task<DaySearchResultDto> SearchAsync(string dayName, int year, CancellationToken cancellationToken)
    {
        var result = new DaySearchResultDto(
            DayNameAr: dayName,
            DayNameEn: null,
            AnnualDate: null,
            OfficialOrganizer: null,
            OfficialOrganizerSource: null,
            HistorySummary: "ملخص مؤقت بانتظار ربط مزوّد البحث بالذكاء الاصطناعي.",
            HistorySource: null,
            CurrentThemeAr: null,
            CurrentThemeEn: null,
            ThemeSourceUrl: null,
            Activations: BuildPlaceholderActivations(year),
            DesignSamples: BuildPlaceholderDesignSamples(year),
            Suggestions: BuildPlaceholderSuggestions(),
            Sources: Array.Empty<DaySearchSourceDto>());

        return Task.FromResult(result);
    }

    private static List<DaySearchActivationDto> BuildPlaceholderActivations(int year) =>
        Enumerable.Range(1, ActivationCount)
            .Select(i => new DaySearchActivationDto(
                EntityName: $"جهة سعودية {i} (بيانات مؤقتة)",
                EntityType: "حكومي",
                ActivationType: "منشور",
                Platform: "موقع رسمي",
                Description: "وصف مؤقت بانتظار ربط مزوّد البحث بالذكاء الاصطناعي",
                SourceUrl: null,
                Country: "السعودية",
                Year: year - 1))
            .ToList();

    private static List<DaySearchDesignSampleDto> BuildPlaceholderDesignSamples(int year) =>
        Enumerable.Range(1, DesignSampleCount)
            .Select(i => new DaySearchDesignSampleDto(
                EntityName: $"جهة {i} (بيانات مؤقتة)",
                EntityType: "حكومي",
                Platform: "موقع رسمي",
                Description: "تصميم مؤقت بانتظار ربط مزوّد البحث",
                PageUrl: null,
                ImageUrl: null,
                Country: "السعودية",
                Year: year - 1))
            .ToList();

    private static List<string> BuildPlaceholderSuggestions() =>
        Enumerable.Range(1, SuggestionCount).Select(i => $"فكرة تفعيل مقترحة {i} (نموذج مؤقت)").ToList();
}
