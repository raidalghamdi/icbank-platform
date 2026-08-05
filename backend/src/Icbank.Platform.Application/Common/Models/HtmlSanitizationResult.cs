namespace Icbank.Platform.Application.Common.Models;

/// <summary>
/// The outcome of running client-supplied HTML through <c>IHtmlSanitizer</c> (SEC-11). Callers
/// must not silently drop content that sanitization changed -- <see cref="WasModified"/> tells
/// the caller whether to write an audit-trail entry recording that the stored value differs from
/// what the client submitted.
/// </summary>
/// <param name="SanitizedHtml">The sanitized HTML, safe to persist and safe to render.</param>
/// <param name="WasModified">
/// <c>true</c> when sanitization removed or altered anything from the original input (a
/// disallowed tag/attribute, an unsafe URL scheme, etc.) -- callers should record this in the
/// audit trail rather than discard the signal.
/// </param>
public sealed record HtmlSanitizationResult(string SanitizedHtml, bool WasModified);
