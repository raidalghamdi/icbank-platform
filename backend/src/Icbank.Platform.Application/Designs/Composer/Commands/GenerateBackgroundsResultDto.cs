namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>The response shape for <c>POST /designs/generate-backgrounds</c>.</summary>
/// <param name="Images">Every successfully-generated variant (partial success is possible -- BUSINESS-RULES.md §7.3's <c>Promise.allSettled</c> semantics).</param>
public sealed record GenerateBackgroundsResultDto(IReadOnlyList<GeneratedBackgroundDto> Images);
