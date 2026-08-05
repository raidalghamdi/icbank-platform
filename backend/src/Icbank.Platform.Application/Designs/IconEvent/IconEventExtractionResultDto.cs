namespace Icbank.Platform.Application.Designs.IconEvent;

/// <summary>The full typed AI response shape (H-2: typed DTO, validated before use).</summary>
/// <param name="Extracted">The extracted event data.</param>
/// <param name="Variants">The 3 proposed design variants.</param>
public sealed record IconEventExtractionResultDto(IconEventExtractedDataDto Extracted, IReadOnlyList<IconEventVariantProposalDto> Variants);
