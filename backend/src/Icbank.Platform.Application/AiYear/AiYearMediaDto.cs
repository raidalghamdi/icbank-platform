namespace Icbank.Platform.Application.AiYear;

/// <summary>Ports a single row of <c>ai_year_media</c> (API-SURFACE.md §13).</summary>
/// <param name="Id">The media row id.</param>
/// <param name="ObjectPath">The storage object path.</param>
/// <param name="FileName">The original file name, if known.</param>
/// <param name="ContentType">The MIME content type, if known.</param>
/// <param name="SortOrder">The display sort order.</param>
public sealed record AiYearMediaDto(int Id, string ObjectPath, string? FileName, string? ContentType, int SortOrder);
