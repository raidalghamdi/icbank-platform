using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Common.Models;

namespace Icbank.Platform.Infrastructure.Security;

/// <summary>
/// Default <see cref="Icbank.Platform.Application.Common.Interfaces.IHtmlSanitizer"/> implementation, backed by Ganss.Xss's
/// <c>HtmlSanitizer</c> (MIT, pinned to 9.0.892 -- see <c>Directory.Packages.props</c>). Closes
/// SEC-11: "stored HTML is never sanitized". The allowlist below covers only the formatting an
/// Arabic rich-text section editor genuinely needs (paragraphs, headings, emphasis, lists, basic
/// tables, links, line breaks) -- everything else, including every tag/attribute/URL-scheme the
/// task calls out by name (<c>script</c>, <c>style</c>, <c>iframe</c>, <c>object</c>,
/// <c>embed</c>, <c>form</c>, event-handler attributes, <c>javascript:</c>/<c>data:</c> URLs), is
/// stripped by the underlying library's default-deny behavior once we remove it from the
/// allowlist. Ganss.Xss already rejects <c>javascript:</c>/<c>data:</c> (and every other
/// non-http(s)/mailto scheme) by default via <see cref="Ganss.Xss.HtmlSanitizer.AllowedSchemes"/>;
/// this type only narrows the tag/attribute allowlist further and reports whether sanitization
/// changed anything, per the task's "do not silently drop content without an audit trail"
/// instruction.
/// </summary>
public sealed class GanssHtmlSanitizer : Icbank.Platform.Application.Common.Interfaces.IHtmlSanitizer
{
    private static readonly HashSet<string> AllowedFormattingTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "p", "br", "strong", "b", "em", "i", "u", "s", "span", "div",
        "h1", "h2", "h3", "h4", "h5", "h6",
        "ul", "ol", "li",
        "a", "blockquote",
        "table", "thead", "tbody", "tr", "th", "td",
    };

    private static readonly HashSet<string> AllowedFormattingAttributes = new(StringComparer.OrdinalIgnoreCase)
    {
        "href", "dir", "lang",
    };

    private readonly Ganss.Xss.HtmlSanitizer _sanitizer;

    /// <summary>Initializes a new instance of the <see cref="GanssHtmlSanitizer"/> class.</summary>
    public GanssHtmlSanitizer()
    {
        _sanitizer = new Ganss.Xss.HtmlSanitizer();
        ApplyAllowlist();
    }

    /// <inheritdoc />
    public HtmlSanitizationResult Sanitize(string html)
    {
        var sanitized = _sanitizer.Sanitize(html);
        var wasModified = !string.Equals(NormalizeForComparison(html), NormalizeForComparison(sanitized), StringComparison.Ordinal);
        return new HtmlSanitizationResult(sanitized, wasModified);
    }

    /// <summary>Collapses whitespace-only differences so trivial re-serialization isn't reported as a content change.</summary>
    private static string NormalizeForComparison(string value) => value.Trim();

    /// <summary>
    /// Narrows the library's own (already broad) default allowlist down to only the formatting
    /// tags/attributes an Arabic rich-text section editor needs. Everything not explicitly
    /// allowed here -- <c>script</c>/<c>style</c>/<c>iframe</c>/<c>object</c>/<c>embed</c>/
    /// <c>form</c>/<c>img</c>/<c>svg</c>/every event-handler attribute -- is removed because it is
    /// never added back to <see cref="Ganss.Xss.HtmlSanitizer.AllowedTags"/>/
    /// <see cref="Ganss.Xss.HtmlSanitizer.AllowedAttributes"/>.
    /// </summary>
    private void ApplyAllowlist()
    {
        _sanitizer.AllowedTags.Clear();
        foreach (var tag in AllowedFormattingTags)
        {
            _sanitizer.AllowedTags.Add(tag);
        }

        _sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in AllowedFormattingAttributes)
        {
            _sanitizer.AllowedAttributes.Add(attribute);
        }

        // Why: no CSS property is needed by the editor today; clearing this closes the
        // inline-style url()/expression() vector outright rather than allowlisting properties.
        _sanitizer.AllowedCssProperties.Clear();

        // Why: default scheme allowlist already excludes javascript:/data: -- restated explicitly
        // here so a future edit to this file cannot silently widen it without touching this line.
        _sanitizer.AllowedSchemes.Clear();
        _sanitizer.AllowedSchemes.Add("http");
        _sanitizer.AllowedSchemes.Add("https");
        _sanitizer.AllowedSchemes.Add("mailto");
    }
}
