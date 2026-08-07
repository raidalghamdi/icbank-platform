namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>The upload response shape (API-SURFACE.md §8).</summary>
/// <param name="Processed">The number of files successfully archived.</param>
/// <param name="Results">Per-file outcomes.</param>
public sealed record UploadArchiveDocumentsResultDto(int Processed, IReadOnlyList<UploadedDocumentResultDto> Results);
