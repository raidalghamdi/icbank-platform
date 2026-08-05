using Icbank.Platform.Application.Common.Models;

namespace Icbank.Platform.Application.Common.Interfaces;

/// <summary>
/// Port for sanitizing client-supplied HTML before it is persisted (closes SEC-11: "stored HTML
/// is never sanitized" -- <c>PatchShorfahSectionCommandHandler</c> assigned
/// <c>request.ContentHtml</c> straight onto <c>section.ContentHtml</c> with no sanitizer anywhere
/// in the backend). Every client-supplied HTML field must be passed through
/// <see cref="Sanitize(string)"/> on write, never trusted verbatim, even if nothing renders it
/// server-side today -- the risk is latent stored XSS the moment a renderer or the frontend
/// consumes the raw value.
/// </summary>
public interface IHtmlSanitizer
{
    /// <summary>
    /// Sanitizes a client-supplied HTML fragment against an allowlist of formatting tags/attributes.
    /// Strips <c>script</c>/<c>style</c>/<c>iframe</c>/<c>object</c>/<c>embed</c>/<c>form</c>,
    /// every event-handler attribute (<c>on*</c>), and <c>javascript:</c>/<c>data:</c> URLs.
    /// </summary>
    /// <param name="html">The untrusted, client-supplied HTML.</param>
    /// <returns>
    /// A <see cref="HtmlSanitizationResult"/> carrying the sanitized output and whether the input
    /// was changed by sanitization, so callers can record an audit trail when content was altered.
    /// </returns>
    HtmlSanitizationResult Sanitize(string html);
}
