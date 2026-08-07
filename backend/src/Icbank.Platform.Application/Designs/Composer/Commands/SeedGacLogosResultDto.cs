namespace Icbank.Platform.Application.Designs.Composer.Commands;

/// <summary>The response shape for <c>POST /designs/logos/seed-gac</c>.</summary>
/// <param name="Inserted">The number of newly-inserted logos.</param>
/// <param name="Skipped">The names of logos that already existed and were skipped.</param>
/// <param name="Logos">Every newly-inserted logo.</param>
public sealed record SeedGacLogosResultDto(int Inserted, IReadOnlyList<string> Skipped, IReadOnlyList<BrandLogoDto> Logos);
