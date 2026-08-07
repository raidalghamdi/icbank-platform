namespace Icbank.Platform.Application.Weekend;

/// <summary>The archive-entry list response shape (API-SURFACE.md §8). <c>Preview</c> is the body text truncated to 200 characters, matching the Node source.</summary>
/// <param name="Id">The entry id.</param>
/// <param name="Title">The entry title.</param>
/// <param name="Occasion">The occasion, if any.</param>
/// <param name="Tone">The tone descriptor, if any.</param>
/// <param name="SourceFile">The originating source file name, if imported.</param>
/// <param name="CreatedAt">The UTC creation timestamp.</param>
/// <param name="Preview">The body text truncated to 200 characters.</param>
public sealed record ArchiveEntryDto(int Id, string Title, string? Occasion, string? Tone, string? SourceFile, DateTime CreatedAt, string Preview);
