namespace Icbank.Platform.Api.Controllers;

/// <summary>Request body for <c>PATCH /api/v1/shorfah/media/{mediaId}</c>.</summary>
/// <param name="CaptionAr">The new Arabic caption, if changing.</param>
/// <param name="DisplayOrder">The new display sort order, if changing.</param>
public sealed record PatchShorfahSectionMediaRequest(string? CaptionAr, int? DisplayOrder);
