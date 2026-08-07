namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>The response shape for the 3 reseed-template-set endpoints.</summary>
/// <param name="Count">The total number of templates in the set (inserted + updated).</param>
/// <param name="Templates">Every resulting template row.</param>
/// <param name="Notes">Per-template notes, e.g. <c>updated: {name}</c> for overwritten rows.</param>
public sealed record ReseedDesignTemplateSetResultDto(int Count, IReadOnlyList<DesignTemplateDto> Templates, IReadOnlyList<string> Notes);
