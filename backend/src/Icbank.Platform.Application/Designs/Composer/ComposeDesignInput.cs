using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.Composer;

/// <summary>Resolved input for the server-side design composer (ports <c>composer.ts</c>'s <c>ComposeInput</c>).</summary>
/// <param name="Template">The template to compose against.</param>
/// <param name="BackgroundBytes">The downloaded background image bytes, empty if none was supplied.</param>
/// <param name="TitleText">The title text to render.</param>
/// <param name="BodyText">The body text to render.</param>
/// <param name="TitleFontSize">The optional title font size override.</param>
/// <param name="BodyFontSize">The optional body font size override.</param>
/// <param name="FontFamily">The optional font family override.</param>
/// <param name="SelectedLogos">The logos to composite, in selection order.</param>
/// <param name="Department">The optional department badge text.</param>
public sealed record ComposeDesignInput(
    DesignTemplate Template,
    byte[] BackgroundBytes,
    string TitleText,
    string BodyText,
    double? TitleFontSize,
    double? BodyFontSize,
    string? FontFamily,
    IReadOnlyList<BrandLogo> SelectedLogos,
    string? Department);
