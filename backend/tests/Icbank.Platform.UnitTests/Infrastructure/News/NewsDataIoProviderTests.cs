using System.Net;
using Icbank.Platform.Application.Gac.News;
using Icbank.Platform.Infrastructure.News;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icbank.Platform.UnitTests.Infrastructure.News;

/// <summary>
/// Covers <see cref="NewsDataIoProvider"/> and <see cref="NewsDataApiKeyResolver"/>. This provider is
/// disabled by default because it does not index the Saudi outlets that cover the Authority, but it
/// stays tested so that enabling it later is a configuration change rather than a code change.
/// </summary>
public sealed class NewsDataIoProviderTests
{
    private const string Payload = """
        {
          "status": "success",
          "totalResults": 2,
          "results": [
            {
              "title": "هيئة المنافسة تُصدر قرارات التركز الاقتصادي",
              "link": "https://example.com/a",
              "content": "نص المقال الكامل",
              "source_name": "أرقام",
              "pubDate": "2026-08-05 09:12:00"
            },
            {
              "title": "قرار جديد بعدم الممانعة",
              "link": "https://example.com/b",
              "description": "الوصف فقط",
              "source_id": "maaal",
              "pubDate": "2026-08-06 06:40:00"
            }
          ]
        }
        """;

    [Fact]
    public void ProviderKey_MatchesTheConfigurationKey()
    {
        Assert.Equal("newsdata-io", CreateProvider(new StubHttpMessageHandler()).ProviderKey);
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmptyAndDoesNotCallUpstreamWhenNoKeyIsConfigured()
    {
        var handler = new StubHttpMessageHandler(body: Payload);
        NewsDataIoProvider provider = CreateProvider(handler, apiKey: null);

        IReadOnlyList<FetchedNewsItem> items = await provider.FetchAsync(Query(), CancellationToken.None);

        Assert.Empty(items);
        Assert.Null(handler.LastRequestUri);
    }

    [Fact]
    public async Task FetchAsync_SendsTheKeyAndTermAsQueryParameters()
    {
        var handler = new StubHttpMessageHandler(body: Payload);
        NewsDataIoProvider provider = CreateProvider(handler, apiKey: "pub-test-key");

        await provider.FetchAsync(Query(), CancellationToken.None);

        Assert.NotNull(handler.LastRequestUri);
        Assert.Contains("apikey=pub-test-key", handler.LastRequestUri, StringComparison.Ordinal);
        Assert.Contains(Uri.EscapeDataString("هيئة المنافسة"), handler.LastRequestUri, StringComparison.Ordinal);
        Assert.Contains("language=ar", handler.LastRequestUri, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmptyOnAFailureStatusInsteadOfThrowing()
    {
        NewsDataIoProvider provider = CreateProvider(
            new StubHttpMessageHandler(HttpStatusCode.Unauthorized, "{}"), apiKey: "bad");

        Assert.Empty(await provider.FetchAsync(Query(), CancellationToken.None));
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmptyWhenTheUpstreamIsUnreachable()
    {
        NewsDataIoProvider provider = CreateProvider(
            new StubHttpMessageHandler(throwOnSend: new HttpRequestException("dns")), apiKey: "k");

        Assert.Empty(await provider.FetchAsync(Query(), CancellationToken.None));
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmptyWhenTheUpstreamTimesOut()
    {
        NewsDataIoProvider provider = CreateProvider(
            new StubHttpMessageHandler(throwOnSend: new TaskCanceledException("timeout")), apiKey: "k");

        Assert.Empty(await provider.FetchAsync(Query(), CancellationToken.None));
    }

    [Fact]
    public void ParseResponse_MapsContentThenDescriptionAndSourceNameThenSourceId()
    {
        IReadOnlyList<FetchedNewsItem> items = NewsDataIoProvider.ParseResponse(Payload, 50);

        Assert.Equal(2, items.Count);

        Assert.Equal("نص المقال الكامل", items[0].Body);
        Assert.Equal("أرقام", items[0].SourceName);
        Assert.Equal(new DateTimeOffset(2026, 8, 5, 9, 12, 0, TimeSpan.Zero), items[0].PublishedAt);
        Assert.Equal("newsdata-io", items[0].ProviderKey);

        // No "content" on the second item, so "description" is the fallback body,
        // and "source_id" is the fallback outlet name.
        Assert.Equal("الوصف فقط", items[1].Body);
        Assert.Equal("maaal", items[1].SourceName);
    }

    [Fact]
    public void ParseResponse_HonoursMaxItems()
    {
        Assert.Single(NewsDataIoProvider.ParseResponse(Payload, 1));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("{ not json")]
    [InlineData("""{"status":"success"}""")]
    [InlineData("""{"status":"success","results":null}""")]
    [InlineData("""{"status":"error","results":{"message":"invalid key"}}""")]
    public void ParseResponse_ReturnsEmptyForAPayloadWithNoUsableResultsArray(string json)
    {
        Assert.Empty(NewsDataIoProvider.ParseResponse(json, 50));
    }

    [Fact]
    public void ParseResponse_SkipsItemsMissingATitleOrLinkAndToleratesAnUnparseableDate()
    {
        const string json = """
            {"results":[
              {"link":"https://example.com/no-title","title":"  "},
              {"title":"عنوان بلا رابط"},
              {"title":"عنوان صالح","link":"https://example.com/ok","pubDate":"not-a-date"}
            ]}
            """;

        IReadOnlyList<FetchedNewsItem> items = NewsDataIoProvider.ParseResponse(json, 50);

        FetchedNewsItem item = Assert.Single(items);
        Assert.Equal("عنوان صالح", item.Title);
        Assert.Null(item.PublishedAt);
        Assert.Null(item.Body);
        Assert.Equal(string.Empty, item.SourceName);
    }

    [Fact]
    public void Resolve_PrefersNewsdataApiKeyOverTheGenericName()
    {
        IConfiguration configuration = Build(("NEWSDATA_API_KEY", " specific "), ("NEWS_API_KEY", "generic"));

        Assert.Equal("specific", NewsDataApiKeyResolver.Resolve(configuration));
    }

    [Fact]
    public void Resolve_FallsBackToTheGenericNameAndReturnsNullWhenNeitherIsSet()
    {
        Assert.Equal("generic", NewsDataApiKeyResolver.Resolve(Build(("NEWS_API_KEY", "generic"))));
        Assert.Null(NewsDataApiKeyResolver.Resolve(Build(("NEWSDATA_API_KEY", "   "))));
        Assert.Null(NewsDataApiKeyResolver.Resolve(Build()));
    }

    private static NewsSourceQuery Query(int maxItems = 50) => new("هيئة المنافسة", "ar", "SA", 7, maxItems);

    private static IConfiguration Build(params (string Key, string Value)[] values) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(v => new KeyValuePair<string, string?>(v.Key, v.Value)))
            .Build();

    private static NewsDataIoProvider CreateProvider(StubHttpMessageHandler handler, string? apiKey = "key") =>
        new(new HttpClient(handler), new NewsSourceOptions(), apiKey, NullLogger<NewsDataIoProvider>.Instance);
}
