namespace Icbank.Platform.Application.Campaigns.Commands;

/// <summary>A stored campaign file.</summary>
/// <param name="Path">The storage object path, readable through <c>/storage/objects/{path}</c>.</param>
/// <param name="FileName">The original file name.</param>
/// <param name="ContentType">The MIME type.</param>
/// <param name="SizeBytes">The file size in bytes.</param>
public sealed record CampaignFileDto(string Path, string FileName, string ContentType, int SizeBytes);
