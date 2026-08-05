namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>One official GAC logo asset from the seed catalogue (ports <c>composer/seed-gac-assets.ts</c>).</summary>
/// <param name="LogoName">The Arabic logo name, used as the idempotency key.</param>
/// <param name="ContentBase64">The base64-encoded PNG bytes.</param>
/// <param name="Transparent">Whether the logo has a transparent background.</param>
/// <param name="DefaultWidth">The default render width.</param>
public sealed record GacLogoSeedDefinition(string LogoName, string ContentBase64, bool Transparent, int DefaultWidth);
