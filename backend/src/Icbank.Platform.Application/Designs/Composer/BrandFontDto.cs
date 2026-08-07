namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>Read-model DTO for a <see cref="Icbank.Platform.Domain.Designs.BrandFont"/> row.</summary>
/// <param name="Id">The font id.</param>
/// <param name="FontName">The font's display name.</param>
/// <param name="FontFileUrl">The stored object path.</param>
/// <param name="IsDefault">Whether this is the default font.</param>
public sealed record BrandFontDto(int Id, string FontName, string FontFileUrl, bool IsDefault);
