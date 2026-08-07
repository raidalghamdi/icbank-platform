namespace Icbank.Platform.Application.Shorfah;

/// <summary>The Shorfah section-media response shape (API-SURFACE.md §19).</summary>
/// <param name="Id">The media id.</param>
/// <param name="SectionId">The owning section's id.</param>
/// <param name="MediaUrl">The storage object path/URL.</param>
/// <param name="MediaType">The media kind.</param>
/// <param name="CaptionAr">The optional Arabic caption.</param>
/// <param name="DisplayOrder">The display sort order.</param>
public sealed record ShorfahSectionMediaDto(int Id, int SectionId, string MediaUrl, string MediaType, string? CaptionAr, int? DisplayOrder);
