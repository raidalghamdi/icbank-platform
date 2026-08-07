using Icbank.Platform.Application.Designs.Composer;

namespace Icbank.Platform.Infrastructure.Designs;

/// <summary>
/// Default <see cref="IGacLogoSeedCatalog"/> implementation. Ships the same 2 real logo names as
/// the Node source's <c>composer/seed-gac-assets.ts</c> with a tiny valid 1x1 transparent PNG
/// placeholder instead of the ~500KB brand-manual-sourced bitmap, so the idempotent-seed and
/// storage-upload pipeline is fully exercisable without committing proprietary brand artwork to
/// source control (see WAVE3B-PORT-NOTES.md).
/// </summary>
public sealed class CuratedGacLogoSeedCatalog : IGacLogoSeedCatalog
{
    private const string PlaceholderPngBase64 = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAAAAAA6fptVAAAACklEQVR4AWMAAgAABAABINitgAAAAABJRU5ErkJggg==";
    private const int HorizontalDefaultWidth = 360;
    private const int VerticalDefaultWidth = 240;

    /// <inheritdoc />
    public IReadOnlyList<GacLogoSeedDefinition> GetLogos() => new List<GacLogoSeedDefinition>
    {
        new("شعار الهيئة — أفقي (Horizontal)", PlaceholderPngBase64, Transparent: false, HorizontalDefaultWidth),
        new("شعار الهيئة — عمودي (Vertical)", PlaceholderPngBase64, Transparent: false, VerticalDefaultWidth),
    };
}
