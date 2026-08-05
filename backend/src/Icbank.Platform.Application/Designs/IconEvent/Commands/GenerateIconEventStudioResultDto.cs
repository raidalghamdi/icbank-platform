namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>The response shape for <c>POST /designs/icon-event/studio</c>.</summary>
/// <param name="Variants">The rendered HTML for every requested size.</param>
public sealed record GenerateIconEventStudioResultDto(IReadOnlyList<IconEventStudioVariantDto> Variants);
