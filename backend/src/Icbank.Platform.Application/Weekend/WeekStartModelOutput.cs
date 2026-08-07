namespace Icbank.Platform.Application.Weekend;

/// <summary>A single model's generated output.</summary>
/// <param name="ModelName">The generating model name: <c>claude</c>, <c>openai</c>, or <c>gemini</c>.</param>
/// <param name="OutputText">The generated text.</param>
public sealed record WeekStartModelOutput(string ModelName, string OutputText);
