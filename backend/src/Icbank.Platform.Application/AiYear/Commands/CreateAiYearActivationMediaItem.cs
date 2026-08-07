namespace Icbank.Platform.Application.AiYear.Commands;

/// <summary>One media item supplied when creating/updating an activation.</summary>
/// <param name="ObjectPath">The storage object path, validated against <see cref="AiYearMediaPathValidator"/>.</param>
/// <param name="FileName">The original file name, if known.</param>
/// <param name="ContentType">The MIME content type, if known.</param>
/// <param name="SortOrder">The display sort order.</param>
public sealed record CreateAiYearActivationMediaItem(string ObjectPath, string? FileName, string? ContentType, int? SortOrder);
