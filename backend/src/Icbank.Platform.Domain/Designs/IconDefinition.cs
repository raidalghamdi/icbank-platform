namespace Icbank.Platform.Domain.Designs;

/// <summary>
/// One entry of the icon-event icon catalogue (ports <c>composer/icon-library.ts</c>'s
/// <c>ICON_LIBRARY</c> constant, BUSINESS-RULES.md §7.4). The semantic Arabic label and keywords
/// are ported verbatim so the AI-selection guidance in the icon-event generation prompt
/// (<see cref="IconEventExtractionPrompts"/>) remains accurate; the decorative inline-SVG glyph
/// itself is a rendering-layer asset, not business logic, and is out of scope for this port (see
/// WAVE3B-PORT-NOTES.md).
/// </summary>
/// <param name="Name">The stable icon key, e.g. <c>shield</c>.</param>
/// <param name="LabelAr">The Arabic display label.</param>
/// <param name="Category">The category grouping.</param>
/// <param name="Keywords">The Arabic semantic keywords used for icon matching.</param>
public sealed record IconDefinition(string Name, string LabelAr, IconCategory Category, IReadOnlyList<string> Keywords);
