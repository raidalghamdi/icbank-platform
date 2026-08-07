namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>One successfully-generated and stored background variant.</summary>
/// <param name="Url">The stored object path.</param>
/// <param name="Source">The generation source label, e.g. <c>gemini</c>.</param>
public sealed record GeneratedBackgroundDto(string Url, string Source);
