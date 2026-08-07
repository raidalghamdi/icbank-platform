namespace Icbank.Platform.Application.Weekend.Commands;

/// <summary>A single file's upload outcome.</summary>
/// <param name="Id">The created archive entry id, if successful.</param>
/// <param name="Title">The derived entry title, if successful.</param>
/// <param name="WordCount">The extracted text's word count, if successful.</param>
/// <param name="Skipped">The original file name, if skipped because no text could be extracted.</param>
/// <param name="Reason">The skip reason, if skipped.</param>
/// <param name="Error">An error message, if extraction failed unexpectedly.</param>
public sealed record UploadedDocumentResultDto(int? Id, string? Title, int? WordCount, string? Skipped, string? Reason, string? Error);
