namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>Read-model DTO for a <see cref="Icbank.Platform.Domain.Designs.BrandLogo"/> row.</summary>
/// <param name="Id">The logo id.</param>
/// <param name="LogoName">The logo's display name.</param>
/// <param name="FileUrl">The stored object path.</param>
/// <param name="Transparent">Whether the logo has a transparent background.</param>
/// <param name="DefaultWidth">The default render width, if set.</param>
public sealed record BrandLogoDto(int Id, string LogoName, string FileUrl, bool Transparent, int? DefaultWidth);
