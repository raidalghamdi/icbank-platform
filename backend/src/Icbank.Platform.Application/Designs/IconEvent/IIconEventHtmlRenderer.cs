using Icbank.Platform.Domain.Designs;

namespace Icbank.Platform.Application.Designs.IconEvent;

/// <summary>
/// Port for rendering an <see cref="IconEventInput"/> into an HTML poster document (ports
/// <c>renderIconEventDesign()</c> from <c>composer/icon-event-composer.ts</c>, BUSINESS-RULES.md
/// §7.5). The Node source built ~1100 lines of layout-specific CSS/HTML; per the mandated
/// narrow-named-interface + deterministic-placeholder pattern, the full pixel-perfect layout
/// engine is deferred (see WAVE3B-PORT-NOTES.md) and this port's default implementation
/// (<c>EncodedIconEventHtmlRenderer</c>) emits a schema-correct, semantically-equivalent HTML
/// document with every field HTML-encoded (closes the H-1 class of defect the Node source never
/// addressed -- BUSINESS-RULES.md §7.5 explicitly flags "no HTML sanitization occurs at any
/// point in this pipeline" as SEC-12).
/// </summary>
public interface IIconEventHtmlRenderer
{
    /// <summary>Renders the given input into a complete, encoded HTML document.</summary>
    /// <param name="input">The fully-resolved design input.</param>
    /// <returns>The rendered HTML document string.</returns>
    string Render(IconEventInput input);
}
