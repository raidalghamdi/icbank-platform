using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Gac.News;
using Microsoft.Extensions.Logging;

namespace Icbank.Platform.Infrastructure.News;

/// <summary>
/// Reads press coverage from Google News RSS. This is the primary provider because it is the only
/// evaluated source that actually indexes the Saudi business press covering the Authority.
/// </summary>
/// <remarks>
/// Two known limitations, both deliberate trade-offs rather than defects:
/// (1) it is an undocumented endpoint with no service level guarantee, so every failure path here
/// degrades to an empty list and a logged warning instead of an exception; and
/// (2) it returns headline plus link, not article prose, so <see cref="FetchedNewsItem.Body"/> is
/// usually null and the report generator sees the headline only.
/// </remarks>
public sealed partial class GoogleNewsRssProvider : INewsSourceProvider
{
    /// <summary>The configuration key selecting this provider.</summary>
    public const string Key = "google-news-rss";

    private readonly HttpClient _httpClient;
    private readonly NewsSourceOptions _options;
    private readonly ILogger<GoogleNewsRssProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="GoogleNewsRssProvider"/> class.</summary>
    /// <param name="httpClient">The HTTP client used to call Google News.</param>
    /// <param name="options">The news pipeline configuration.</param>
    /// <param name="logger">The logger used to record unreachable-upstream diagnostics.</param>
    public GoogleNewsRssProvider(HttpClient httpClient, NewsSourceOptions options, ILogger<GoogleNewsRssProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ProviderKey => Key;

    /// <inheritdoc />
    public async Task<IReadOnlyList<FetchedNewsItem>> FetchAsync(NewsSourceQuery query, CancellationToken cancellationToken)
    {
        var url = BuildUrl(query);

        string xml;
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogBadStatus(_logger, (int)response.StatusCode, query.Term);
                return Array.Empty<FetchedNewsItem>();
            }

            xml = await response.Content.ReadAsStringAsync(cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            LogUnreachable(_logger, exception, query.Term);
            return Array.Empty<FetchedNewsItem>();
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            LogTimedOut(_logger, exception, query.Term);
            return Array.Empty<FetchedNewsItem>();
        }

        IReadOnlyList<FetchedNewsItem> items = GoogleNewsRssParser.Parse(xml);
        return items.Count <= query.MaxItems ? items : items.Take(query.MaxItems).ToList();
    }

    /// <summary>Builds the RSS search URL for a query.</summary>
    /// <param name="query">The search parameters.</param>
    /// <returns>The absolute request URL.</returns>
    /// <remarks>
    /// The <c>when:Nd</c> operator is appended to the query text itself -- Google News has no separate
    /// date parameter on this endpoint. <c>ceid</c> must agree with <c>hl</c> and <c>gl</c> or Google
    /// silently falls back to US English results, which is how a Saudi Arabic query ends up returning
    /// American headlines.
    /// </remarks>
    internal string BuildUrl(NewsSourceQuery query)
    {
        var language = string.IsNullOrWhiteSpace(query.Language) ? "ar" : query.Language;
        var region = string.IsNullOrWhiteSpace(query.Region) ? "SA" : query.Region;
        var term = Uri.EscapeDataString($"{query.Term} when:{Math.Max(1, query.WithinDays)}d");

        return $"{_options.GoogleNewsBaseUrl.TrimEnd('/')}?q={term}&hl={language}&gl={region}&ceid={region}:{language}";
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Google News RSS returned HTTP {StatusCode} for term {Term}; treating as no results.")]
    private static partial void LogBadStatus(ILogger logger, int statusCode, string term);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Google News RSS unreachable for term {Term}; treating as no results.")]
    private static partial void LogUnreachable(ILogger logger, Exception exception, string term);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Google News RSS timed out for term {Term}; treating as no results.")]
    private static partial void LogTimedOut(ILogger logger, Exception exception, string term);
}
