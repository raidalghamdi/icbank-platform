namespace Icbank.Platform.Application.Weekend;

/// <summary>The generated week-start output response shape (API-SURFACE.md §8).</summary>
/// <param name="Id">The output id.</param>
/// <param name="Topic">The message topic.</param>
/// <param name="ModelName">The generating model name: claude, openai, or gemini.</param>
/// <param name="OutputText">The generated output text.</param>
/// <param name="Selected">Whether this is the human-approved draft.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
public sealed record GeneratedOutputDto(int Id, string Topic, string ModelName, string OutputText, bool Selected, DateTime CreatedAt);
