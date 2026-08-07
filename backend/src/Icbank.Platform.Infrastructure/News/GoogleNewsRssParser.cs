using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using Icbank.Platform.Application.Gac.News;

namespace Icbank.Platform.Infrastructure.News;

/// <summary>
/// Turns a Google News RSS payload into <see cref="FetchedNewsItem"/> values. Kept separate from
/// the HTTP call so the parsing rules -- which carry all the sharp edges -- are unit-testable
/// against fixture XML with no network involved.
/// </summary>
public static class GoogleNewsRssParser
{
    private const string ProviderKey = GoogleNewsRssProvider.Key;

    /// <summary>Parses an RSS document.</summary>
    /// <param name="xml">The raw RSS XML.</param>
    /// <returns>The parsed items, preserving document order (Google returns newest first).</returns>
    /// <remarks>
    /// Returns an empty list on malformed XML rather than throwing: a single bad response from an
    /// unofficial endpoint must not fail the whole scheduled fetch.
    /// </remarks>
    public static IReadOnlyList<FetchedNewsItem> Parse(string xml)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            return Array.Empty<FetchedNewsItem>();
        }

        XDocument document;
        try
        {
            document = XDocument.Parse(xml);
        }
        catch (System.Xml.XmlException)
        {
            return Array.Empty<FetchedNewsItem>();
        }

        var items = new List<FetchedNewsItem>();
        foreach (XElement element in document.Descendants("item"))
        {
            FetchedNewsItem? item = ParseItem(element);
            if (item is not null)
            {
                items.Add(item);
            }
        }

        return items;
    }

    /// <summary>
    /// Strips the outlet suffix Google appends to every headline.
    /// </summary>
    /// <param name="title">The raw <c>&lt;title&gt;</c> text.</param>
    /// <param name="sourceName">The outlet name from the <c>&lt;source&gt;</c> element.</param>
    /// <returns>The headline without its trailing outlet attribution.</returns>
    /// <remarks>
    /// Google formats titles as <c>Headline - Outlet</c>. Some Arabic outlets already end their own
    /// headline with the paper's name, producing <c>Headline - جريدة المدينة - جريدة المدينة</c>, so
    /// the suffix is stripped repeatedly rather than once. Matching is done against the known outlet
    /// name instead of "split on the last dash", because Arabic headlines legitimately contain dashes.
    /// </remarks>
    public static string StripOutletSuffix(string title, string sourceName)
    {
        var result = title.Trim();
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            return result;
        }

        var suffix = " - " + sourceName.Trim();
        while (result.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
        {
            result = result[..^suffix.Length].TrimEnd();
        }

        return result.Length == 0 ? title.Trim() : result;
    }

    private static FetchedNewsItem? ParseItem(XElement element)
    {
        var link = (string?)element.Element("link");
        var rawTitle = (string?)element.Element("title");
        if (string.IsNullOrWhiteSpace(link) || string.IsNullOrWhiteSpace(rawTitle))
        {
            return null;
        }

        XElement? sourceElement = element.Element("source");
        var sourceName = sourceElement?.Value?.Trim() ?? string.Empty;
        var title = StripOutletSuffix(WebUtility.HtmlDecode(rawTitle), sourceName);
        var body = ExtractBody((string?)element.Element("description"));

        return new FetchedNewsItem(
            title,
            body,
            link.Trim(),
            sourceName,
            ParsePubDate((string?)element.Element("pubDate")),
            ProviderKey);
    }

    /// <summary>
    /// Reduces the HTML blob Google puts in <c>&lt;description&gt;</c> to plain text, or returns null
    /// when nothing usable survives. Google's description is a link list rather than article prose,
    /// so callers should treat a null body as expected, not as an error.
    /// </summary>
    private static string? ExtractBody(string? description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            return null;
        }

        var text = WebUtility.HtmlDecode(Regex.Replace(description, "<.*?>", " ", RegexOptions.Singleline));
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length == 0 ? null : text;
    }

    private static DateTimeOffset? ParsePubDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTimeOffset parsed)
            ? parsed.ToUniversalTime()
            : null;
    }
}
