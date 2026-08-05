namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// Port for AI section-content generation (BUSINESS-RULES.md §1.8, <c>shorfah.ts:471-513</c>).
/// The Node source called Gemini (<c>geminiJSON()</c>, 2000 max tokens); wiring a real provider is
/// deferred (see <c>TemplateShorfahSectionContentGenerator</c>), following the same deferral
/// pattern as every other AI port in this codebase.
/// </summary>
public interface IShorfahSectionContentGenerator
{
    /// <summary>Generates section content from the given prompt.</summary>
    /// <param name="prompt">The full envelope prompt (see <see cref="ShorfahGenerationPrompts.BuildPrompt"/>).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The generated content, typed and ready for FluentValidation.</returns>
    Task<ShorfahGeneratedSectionContent> GenerateAsync(string prompt, CancellationToken cancellationToken);
}
