using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>Maps <see cref="BrandLogo"/>/<see cref="BrandFont"/> entities to their read-model DTOs.</summary>
public static class BrandAssetMapper
{
    /// <summary>Maps a logo entity.</summary>
    /// <param name="entity">The entity to map.</param>
    /// <returns>The mapped DTO.</returns>
    public static BrandLogoDto ToDto(BrandLogo entity) => new(entity.Id, entity.LogoName, entity.FileUrl, entity.Transparent, entity.DefaultWidth);

    /// <summary>Maps a font entity.</summary>
    /// <param name="entity">The entity to map.</param>
    /// <returns>The mapped DTO.</returns>
    public static BrandFontDto ToDto(BrandFont entity) => new(entity.Id, entity.FontName, entity.FontFileUrl, entity.IsDefault);
}
