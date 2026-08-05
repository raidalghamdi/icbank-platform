using Icbank.Platform.Application.Designs.IconEvent;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>
/// Deterministic, non-AI default <see cref="IIconEventDesignExtractor"/> implementation. The
/// Node source called a Gemini-chain-then-Perplexity fallback (<c>aiJSONWithFallback</c>); wiring
/// a real provider is deferred for this wave (see WAVE3B-PORT-NOTES.md), matching the same
/// placeholder pattern as <c>TemplateInternationalDaySearchProvider</c>. This implementation
/// returns a schema-correct, clearly-labeled result with an empty <c>stats</c> array (never
/// fabricating numbers, honoring the anti-hallucination intent even in the placeholder path) and
/// 3 layout proposals so the handler's diversity/typography-guarantee post-processing rules are
/// still exercised end-to-end.
/// </summary>
public sealed class TemplateIconEventDesignExtractor : IIconEventDesignExtractor
{
    private static readonly string[] PlaceholderSupportingIcons = { "calendar", "clock", "map-pin" };

    /// <inheritdoc />
    public Task<IconEventExtractionResultDto> ExtractAsync(string prompt, CancellationToken cancellationToken)
    {
        var extracted = new IconEventExtractedDataDto(
            Headline: "عنوان مؤقت بانتظار ربط مزوّد الذكاء الاصطناعي",
            Subtitle: string.Empty,
            Department: string.Empty,
            Hashtag: string.Empty,
            ContactEmail: string.Empty,
            ContactPhone: string.Empty,
            Stats: Array.Empty<IconEventStatDto>());

        var variants = new List<IconEventVariantProposalDto>
        {
            new("stats-hero", "sparkles", PlaceholderSupportingIcons, "نموذج مؤقت بانتظار ربط المزوّد"),
            new("hero", "sparkles", PlaceholderSupportingIcons, "نموذج مؤقت بانتظار ربط المزوّد"),
            new("split", "sparkles", PlaceholderSupportingIcons, "نموذج مؤقت بانتظار ربط المزوّد"),
        };

        return Task.FromResult(new IconEventExtractionResultDto(extracted, variants));
    }
}
