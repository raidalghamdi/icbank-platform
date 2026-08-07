using System.Globalization;
using System.Text.Json;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Application.Gac.News;
using Microsoft.Extensions.Logging;

namespace Icbank.Platform.Infrastructure.News;

/// <summary>
/// Reads press coverage from NewsData.io. Implemented and registered, but intentionally absent from
/// <see cref="NewsSourceOptions.EnabledProviders"/> by default.
/// </summary>
/// <remarks>
/// Kept in the codebase despite failing evaluation so that the abstraction has a second real
/// implementation and the swap is proven to work. The August 2026 test against a live key: the query
/// <c>هيئة المنافسة</c> with <c>language=ar</c> returned zero results with and without
/// <c>country=sa</c>; <c>country=sa</c> alone returned 2,081 items led by Sudanese, Egyptian and
/// Dutch football stories; and the advertised 84 Saudi sources included Asia Times, Il Sole 24 Ore
/// and InDaily Queensland while omitting every Saudi business outlet that covers the Authority.
/// Their paid tiers add request volume and archive depth, not sources, so upgrading would not change
/// the outcome. Enable this only if NewsData.io later indexes أرقام / صحيفة مال / الاقتصادية.
/// </remarks>
public sealed partial class NewsDataIoProvider : INewsSourceProvider
{
    /// <summary>The configuration key selecting this provider.</summary>
    public const string Key = "newsdata-io";

    private readonly HttpClient _httpClient;
    private readonly NewsSourceOptions _options;
    private readonly string? _apiKey;
    private readonly ILogger<NewsDataIoProvider> _logger;

    /// <summary>Initializes a new instance of the <see cref="NewsDataIoProvider"/> class.</summary>
    /// <param name="httpClient">The HTTP client used to call NewsData.io.</param>
    /// <param name="options">The news pipeline configuration.</param>
    /// <param name="apiKey">The resolved API key, or null when unconfigured.</param>
    /// <param name="logger">The logger used to record unreachable-upstream diagnostics.</param>
    public NewsDataIoProvider(
        HttpClient httpClient,
        NewsSourceOptions options,
        string? apiKey,
        ILogger<NewsDataIoProvider> logger)
    {
        _httpClient = httpClient;
        _options = options;
        _apiKey = apiKey;
        _logger = logger;
    }

    /// <inheritdoc />
    public string ProviderKey => Key;

    /// <inheritdoc />
    public async Task<IReadOnlyList<FetchedNewsItem>> FetchAsync(NewsSourceQuery query, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            LogMissingKey(_logger, query.Term);
            return Array.Empty<FetchedNewsItem>();
        }

        var url = $"{_options.NewsDataBaseUrl}?apikey={Uri.EscapeDataString(_apiKey)}" +
            $"&q={Uri.EscapeDataString(query.Term)}&language={Uri.EscapeDataString(query.Language)}";

        string json;
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                LogBadStatus(_logger, (int)response.StatusCode, query.Term);
                return Array.Empty<FetchedNewsItem>();
            }

            json = await response.Content.ReadAsStringAsync(cancellationToken);
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

        return ParseResponse(json, query.MaxItems);
    }

    /// <summary>Maps a NewsData.io payload onto provider-neutral items.</summary>
    /// <param name="json">The raw response body.</param>
    /// <param name="maxItems">The cap on returned items.</param>
    /// <returns>The parsed items.</returns>
    internal static IReadOnlyList<FetchedNewsItem> ParseResponse(string json, int maxItems)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return Array.Empty<FetchedNewsItem>();
        }

        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(json);
        }
        catch (JsonException)
        {
            return Array.Empty<FetchedNewsItem>();
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("results", out JsonElement results) ||
                results.ValueKind != JsonValueKind.Array)
            {
                return Array.Empty<FetchedNewsItem>();
            }

            var items = new List<FetchedNewsItem>();
            foreach (JsonElement element in results.EnumerateArray())
            {
                if (items.Count >= maxItems)
                {
                    break;
                }

                FetchedNewsItem? item = ParseItem(element);
                if (item is not null)
                {
                    items.Add(item);
                }
            }

            return items;
        }
    }

    private static FetchedNewsItem? ParseItem(JsonElement element)
    {
        var title = ReadString(element, "title");
        var link = ReadString(element, "link");
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(link))
        {
            return null;
        }

        var body = ReadString(element, "content") ?? ReadString(element, "description");

        return new FetchedNewsItem(
            title.Trim(),
            string.IsNullOrWhiteSpace(body) ? null : body.Trim(),
            link.Trim(),
            ReadString(element, "source_name") ?? ReadString(element, "source_id") ?? string.Empty,
            ParsePubDate(ReadString(element, "pubDate")),
            Key);
    }

    private static string? ReadString(JsonElement element, string propertyName) =>
        element.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

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

    [LoggerMessage(Level = LogLevel.Warning, Message = "NewsData.io is enabled but no API key is configured (checked NEWSDATA_API_KEY, NEWS_API_KEY); skipping term {Term}.")]
    private static partial void LogMissingKey(ILogger logger, string term);

    [LoggerMessage(Level = LogLevel.Warning, Message = "NewsData.io returned HTTP {StatusCode} for term {Term}; treating as no results.")]
    private static partial void LogBadStatus(ILogger logger, int statusCode, string term);

    [LoggerMessage(Level = LogLevel.Warning, Message = "NewsData.io unreachable for term {Term}; treating as no results.")]
    private static partial void LogUnreachable(ILogger logger, Exception exception, string term);

    [LoggerMessage(Level = LogLevel.Warning, Message = "NewsData.io timed out for term {Term}; treating as no results.")]
    private static partial void LogTimedOut(ILogger logger, Exception exception, string term);
}
