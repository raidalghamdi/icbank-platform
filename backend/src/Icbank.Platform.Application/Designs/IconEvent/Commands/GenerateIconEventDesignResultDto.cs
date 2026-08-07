namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>The full response shape for <c>POST /designs/icon-event/generate</c>.</summary>
/// <param name="Variants">The 3 generated design variants.</param>
/// <param name="Count">The variant count (always 3).</param>
/// <param name="Extracted">The AI-extracted event data, for the client to display/edit.</param>
/// <param name="Warning">A non-null warning message when the deterministic local fallback was used instead of the AI extractor.</param>
public sealed record GenerateIconEventDesignResultDto(
    IReadOnlyList<IconEventVariantDto> Variants, int Count, IconEventExtractedDataDto Extracted, string? Warning);
