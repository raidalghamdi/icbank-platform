namespace Icbank.Platform.Application.Shorfah;

/// <summary>
/// The typed shape an AI section-generation response must deserialise into and pass validation
/// against before persistence (task requirement H-2 class: "any AI JSON must deserialise into a
/// typed DTO and pass FluentValidation before persistence"). Mirrors the Node source's expected
/// <c>{ content_md: string }</c> shape (BUSINESS-RULES.md §1.8).
/// </summary>
/// <param name="ContentMd">The generated markdown content body.</param>
public sealed record ShorfahGeneratedSectionContent(string ContentMd);
