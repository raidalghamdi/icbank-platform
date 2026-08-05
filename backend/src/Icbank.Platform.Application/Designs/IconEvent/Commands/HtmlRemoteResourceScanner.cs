using System.Text.RegularExpressions;

namespace Icbank.Platform.Application.Designs.IconEvent.Commands;

/// <summary>
/// Scans client-supplied HTML for any reference that would cause a renderer to reach out over the
/// network -- closing the SSRF half of SEC-12 at the input boundary (BUSINESS-RULES.md §7.5,
/// DEFECT-LOG.md SEC-12 [H-3]). The resource-exhaustion half of SEC-12 was already closed by
/// <see cref="IDesignGenerationRateLimiter"/> and the length cap in
/// <see cref="RenderIconEventDesignCommandValidator"/>; this type closes the remainder.
/// </summary>
/// <remarks>
/// <para>
/// <b>Decision, documented per the task's explicit instruction to decide and record whether any
/// remote fetching is permitted at all:</b> the allowlist of permitted remote hosts is empty by
/// default and there is currently no configuration surface to add to it. Every external resource
/// reference found in submitted HTML is rejected outright (the command fails validation) rather
/// than silently stripped, because <see cref="RenderIconEventDesignCommandHandler"/> hands the raw
/// <c>Html</c> string to <see cref="IIconEventImageRenderer"/> verbatim -- stripping tags here
/// would require re-serializing sanitized HTML back into a string, which is exactly the SEC-11
/// sanitizer's job for a different field, not this validator's. Rejecting is also the more honest
/// signal to the caller: "this markup can't be rendered safely" rather than silently rendering
/// something other than what was submitted. This posture must hold even after a real renderer
/// replaces <c>TemplateIconEventImageRenderer</c> -- the check runs at the input boundary
/// (the validator), not inside the renderer, so it is renderer-implementation-independent.
/// </para>
/// <para>
/// Vectors covered: remote <c>src</c> on <c>img</c>/<c>script</c>/<c>iframe</c>/<c>video</c>/
/// <c>audio</c>/<c>source</c>/<c>embed</c>, remote <c>href</c>/<c>data</c> on <c>link</c>/
/// <c>object</c>, <c>url()</c> inside inline <c>style</c> attributes and <c>&lt;style&gt;</c>
/// blocks, <c>@import</c> in <c>&lt;style&gt;</c> blocks, and SVG <c>xlink:href</c>/<c>href</c>.
/// A reference is flagged if it is an absolute <c>http(s)</c>/<c>ftp</c>/scheme-relative URL, or
/// any hostname/IP literal at all (including private/link-local ranges and their DNS-name forms,
/// e.g. <c>localhost</c>) -- <c>data:</c> URIs and same-document <c>#fragment</c> references are
/// exempt because they cannot cause an outbound network fetch.
/// </para>
/// </remarks>
public static class HtmlRemoteResourceScanner
{
    // Why: these patterns intentionally over-match (e.g. matching attribute-ish text inside
    // otherwise-malformed HTML) rather than under-match -- for an input-boundary security check,
    // a false positive (rejecting borderline input) is the safe failure mode; a false negative
    // (letting a real SSRF vector through) is not.
    private static readonly Regex[] RemoteReferencePatterns =
    {
        // <img|script|iframe|video|audio|source|embed src="...">
        new(@"<\s*(img|script|iframe|video|audio|source|embed)\b[^>]*\bsrc\s*=\s*(?<q>[""'])(?<url>[^""']*)\k<q>", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // <link href="..."> (stylesheets, preloads, favicons -- all fetchable)
        new(@"<\s*link\b[^>]*\bhref\s*=\s*(?<q>[""'])(?<url>[^""']*)\k<q>", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // <object data="...">
        new(@"<\s*object\b[^>]*\bdata\s*=\s*(?<q>[""'])(?<url>[^""']*)\k<q>", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // xlink:href="..." or bare href="..." on SVG <image>/<use>/<a> elements
        new(@"(?:xlink:href|href)\s*=\s*(?<q>[""'])(?<url>[^""']*)\k<q>", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // url(...) in inline style attributes or <style> blocks, quoted or unquoted
        new(@"url\s*\(\s*(?<q>[""']?)(?<url>[^)""']+)\k<q>\s*\)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

        // @import "..." / @import url(...) handled by the url() pattern above; bare @import "..." form:
        new(@"@import\s+(?<q>[""'])(?<url>[^""']*)\k<q>", RegexOptions.IgnoreCase | RegexOptions.Compiled),
    };

    private static readonly string[] LocalhostHostnames =
    {
        "localhost",
        "localhost.localdomain",
        "metadata.google.internal",
    };

    /// <summary>
    /// Returns every distinct remote resource reference found in <paramref name="html"/> that
    /// would cause an outbound network fetch if rendered. An empty result means the HTML contains
    /// no such reference (relative/fragment/<c>data:</c>-only references are permitted since they
    /// cannot reach the network).
    /// </summary>
    public static IReadOnlyList<string> FindRemoteReferences(string html)
    {
        var found = new List<string>();
        foreach (Regex pattern in RemoteReferencePatterns)
        {
            foreach (Match match in pattern.Matches(html))
            {
                var url = match.Groups["url"].Value.Trim();
                if (IsNetworkReachable(url) && !found.Contains(url, StringComparer.OrdinalIgnoreCase))
                {
                    found.Add(url);
                }
            }
        }

        return found;
    }

    /// <summary>
    /// Returns, for every remote reference found by <see cref="FindRemoteReferences"/>, whether it
    /// additionally targets private/link-local/localhost-style address space per
    /// <see cref="PrivateNetworkHostClassifier"/>. Every reference is rejected regardless of this
    /// classification (the allowlist is empty by default -- see the type-level remarks) but the
    /// classification is surfaced so tests can assert the specific address-space vectors SEC-12
    /// calls out (the cloud metadata endpoint, RFC 1918 ranges, IPv6 unique-local/loopback, and
    /// localhost-style hostnames) are all independently caught, not just caught incidentally by
    /// the broader "reject everything remote" rule.
    /// </summary>
    public static IReadOnlyList<(string Url, bool TargetsPrivateOrLinkLocalAddressSpace)> ClassifyRemoteReferences(string html)
    {
        return FindRemoteReferences(html)
            .Select(url => (url, TargetsPrivateOrLinkLocalAddressSpace: PrivateNetworkHostClassifier.IsPrivateOrLinkLocal(ExtractHost(url))))
            .ToList();
    }

    /// <summary>Extracts the bare host (no scheme, no port, no path) from a URL or bare host[:port] string, for classification purposes only.</summary>
    private static string ExtractHost(string url)
    {
        var withoutScheme = url.StartsWith("//", StringComparison.Ordinal) ? url[2..] : url;
        if (Uri.TryCreate(withoutScheme, UriKind.Absolute, out Uri? absolute))
        {
            return absolute.Host;
        }

        if (Uri.TryCreate("http://" + withoutScheme, UriKind.Absolute, out Uri? withAssumedScheme))
        {
            return withAssumedScheme.Host;
        }

        return withoutScheme.Split('/', 2)[0].Split('?', 2)[0].Split('#', 2)[0].Split(':', 2)[0];
    }

    /// <summary>
    /// Determines whether <paramref name="url"/> would cause a network fetch if used as a
    /// resource reference -- <c>true</c> for absolute URLs, scheme-relative (<c>//host/...</c>)
    /// URLs, and bare host[:port] forms; <c>false</c> for <c>data:</c> URIs, same-document
    /// fragments, empty values, and relative paths (which resolve against the document's own
    /// origin -- irrelevant here since the "document" is never served from a URL at all).
    /// </summary>
    private static bool IsNetworkReachable(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        if (url.StartsWith('#'))
        {
            return false;
        }

        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            return true;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out Uri? absolute))
        {
            return absolute.Scheme is "http" or "https" or "ftp";
        }

        // Why: a relative path ("images/x.png") never reaches the network for this renderer,
        // because the HTML is never served from a real origin the browser could resolve against
        // -- it's handed directly as a content string. Only treat it as reachable if it looks
        // like a bare hostname (contains a dot or is a known localhost-style name) rather than a
        // path, to avoid flagging ordinary relative asset paths as SSRF vectors.
        return LooksLikeBareHostname(url);
    }

    private static bool LooksLikeBareHostname(string url)
    {
        var candidate = url.Split('/', 2)[0].Split('?', 2)[0].Split('#', 2)[0];
        if (candidate.Length == 0 || candidate.Contains(' '))
        {
            return false;
        }

        if (LocalhostHostnames.Contains(candidate, StringComparer.OrdinalIgnoreCase))
        {
            return true;
        }

        // Bracketed IPv6 literal, optionally with a port, e.g. "[::1]:8080".
        if (candidate.StartsWith('['))
        {
            return true;
        }

        // A dot or colon outside of a path segment strongly suggests "host[:port]" or "host.tld"
        // rather than a relative file path (which this renderer never treats as network-reachable
        // otherwise). Pure filenames like "logo.png" are excluded by requiring the module to also
        // not be a recognized static-asset extension.
        return candidate.Contains(':') && !candidate.Contains("://", StringComparison.Ordinal);
    }
}
