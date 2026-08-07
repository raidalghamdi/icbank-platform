using System.Net;
using Icbank.Platform.Application.Gac.News;
using Icbank.Platform.Infrastructure.News;
using Microsoft.Extensions.Logging.Abstractions;

namespace Icbank.Platform.UnitTests.Infrastructure.News;

/// <summary>
/// Covers <see cref="GoogleNewsRssProvider"/>: the URL it builds, and its contract that every
/// upstream failure degrades to an empty list rather than throwing. The provider calls an
/// undocumented endpoint, so a thrown exception would take down a whole scheduled fetch.
/// </summary>
public sealed class GoogleNewsRssProviderTests
{
    private const string FeedXml = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0"><channel>
          <item>
            <title>هيئة المنافسة تُصدر 34 قراراً بخصوص طلبات التركز الاقتصادي - أرقام</title>
            <link>https://www.argaam.com/ar/article/articledetail/id/1800001</link>
            <pubDate>Wed, 05 Aug 2026 09:12:00 GMT</pubDate>
            <source url="https://www.argaam.com">أرقام</source>
            <description>&lt;a href="x"&gt;عنوان&lt;/a&gt;</description>
          </item>
          <item>
            <title>الهيئة العامة للمنافسة توافق على طلب تركز - صحيفة مال</title>
            <link>https://www.maaal.com/archives/20260806/900002</link>
            <pubDate>Thu, 06 Aug 2026 06:40:00 GMT</pubDate>
            <source url="https://www.maaal.com">صحيفة مال</source>
            <description>&lt;a href="y"&gt;عنوان&lt;/a&gt;</description>
          </item>
        </channel></rss>
        """;

    [Fact]
    public void ProviderKey_MatchesTheConfigurationKey()
    {
        GoogleNewsRssProvider provider = CreateProvider(new StubHttpMessageHandler());

        Assert.Equal("google-news-rss", provider.ProviderKey);
        Assert.Equal(GoogleNewsRssProvider.Key, provider.ProviderKey);
    }

    [Fact]
    public void BuildUrl_AppendsTheDateWindowToTheQueryAndAlignsCeidWithLanguageAndRegion()
    {
        GoogleNewsRssProvider provider = CreateProvider(new StubHttpMessageHandler());

        var url = provider.BuildUrl(Query(withinDays: 7));

        // when:7d must ride inside q= -- this endpoint has no separate date parameter.
        Assert.Contains(Uri.EscapeDataString("هيئة المنافسة العامة when:7d"), url, StringComparison.Ordinal);
        Assert.Contains("&hl=ar", url, StringComparison.Ordinal);
        Assert.Contains("&gl=SA", url, StringComparison.Ordinal);

        // ceid disagreeing with hl/gl is what silently yields US English results.
        Assert.Contains("&ceid=SA:ar", url, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUrl_FallsBackToArabicSaudiWhenLanguageOrRegionIsBlank()
    {
        GoogleNewsRssProvider provider = CreateProvider(new StubHttpMessageHandler());

        var url = provider.BuildUrl(new NewsSourceQuery("منافسة", "  ", string.Empty, 7, 50));

        Assert.Contains("&hl=ar", url, StringComparison.Ordinal);
        Assert.Contains("&ceid=SA:ar", url, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildUrl_ClampsANonPositiveWindowToOneDay()
    {
        GoogleNewsRssProvider provider = CreateProvider(new StubHttpMessageHandler());

        var url = provider.BuildUrl(Query(withinDays: 0));

        Assert.Contains(Uri.EscapeDataString("when:1d"), url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FetchAsync_ReturnsTheParsedFeed()
    {
        GoogleNewsRssProvider provider = CreateProvider(new StubHttpMessageHandler(body: FeedXml));

        IReadOnlyList<FetchedNewsItem> items = await provider.FetchAsync(Query(), CancellationToken.None);

        Assert.Equal(2, items.Count);
        Assert.Equal("أرقام", items[0].SourceName);
        Assert.Equal("google-news-rss", items[0].ProviderKey);

        // The outlet suffix Google appends to every title must be gone.
        Assert.DoesNotContain(" - أرقام", items[0].Title, StringComparison.Ordinal);
    }

    [Fact]
    public async Task FetchAsync_CapsTheResultsAtMaxItems()
    {
        GoogleNewsRssProvider provider = CreateProvider(new StubHttpMessageHandler(body: FeedXml));

        IReadOnlyList<FetchedNewsItem> items = await provider.FetchAsync(
            Query(maxItems: 1), CancellationToken.None);

        Assert.Single(items);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.NotFound)]
    public async Task FetchAsync_ReturnsEmptyOnAFailureStatusInsteadOfThrowing(HttpStatusCode statusCode)
    {
        GoogleNewsRssProvider provider = CreateProvider(new StubHttpMessageHandler(statusCode, "nope"));

        IReadOnlyList<FetchedNewsItem> items = await provider.FetchAsync(Query(), CancellationToken.None);

        Assert.Empty(items);
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmptyWhenTheUpstreamIsUnreachable()
    {
        var handler = new StubHttpMessageHandler(throwOnSend: new HttpRequestException("dns"));
        GoogleNewsRssProvider provider = CreateProvider(handler);

        IReadOnlyList<FetchedNewsItem> items = await provider.FetchAsync(Query(), CancellationToken.None);

        Assert.Empty(items);
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmptyWhenTheUpstreamTimesOut()
    {
        var handler = new StubHttpMessageHandler(throwOnSend: new TaskCanceledException("timeout"));
        GoogleNewsRssProvider provider = CreateProvider(handler);

        IReadOnlyList<FetchedNewsItem> items = await provider.FetchAsync(Query(), CancellationToken.None);

        Assert.Empty(items);
    }

    [Fact]
    public async Task FetchAsync_ReturnsEmptyOnAMalformedBodyInsteadOfThrowing()
    {
        GoogleNewsRssProvider provider = CreateProvider(new StubHttpMessageHandler(body: "<rss><channel"));

        IReadOnlyList<FetchedNewsItem> items = await provider.FetchAsync(Query(), CancellationToken.None);

        Assert.Empty(items);
    }

    private static NewsSourceQuery Query(int maxItems = 50, int withinDays = 7) =>
        new("هيئة المنافسة العامة", "ar", "SA", withinDays, maxItems);

    private static GoogleNewsRssProvider CreateProvider(StubHttpMessageHandler handler) =>
        new(new HttpClient(handler), new NewsSourceOptions(), NullLogger<GoogleNewsRssProvider>.Instance);
}
