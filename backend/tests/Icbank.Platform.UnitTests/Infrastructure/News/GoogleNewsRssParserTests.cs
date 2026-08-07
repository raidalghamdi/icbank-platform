using FluentAssertions;
using Icbank.Platform.Application.Gac.News;
using Icbank.Platform.Infrastructure.News;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.News;

/// <summary>
/// Verifies <see cref="GoogleNewsRssParser"/> against fixture XML shaped like the real Saudi-Arabic
/// feed, including the duplicated outlet suffix that أرقام and جريدة المدينة actually emit.
/// </summary>
public sealed class GoogleNewsRssParserTests
{
    private const string Feed = """
        <?xml version="1.0" encoding="UTF-8"?>
        <rss version="2.0"><channel>
          <item>
            <title>هيئة المنافسة تتلقى 112 طلب تركز اقتصادي في الربع الثاني 2026 - أرقام</title>
            <link>https://www.argaam.com/ar/article/1</link>
            <pubDate>Mon, 03 Aug 2026 09:30:00 GMT</pubDate>
            <description>&lt;a href="https://www.argaam.com/ar/article/1"&gt;تفاصيل الطلبات&lt;/a&gt;</description>
            <source url="https://www.argaam.com">أرقام</source>
          </item>
          <item>
            <title>الهيئة العامة للمنافسة تُصدر 34 قراراً بعدم الممانعة - جريدة المدينة - جريدة المدينة</title>
            <link>https://www.al-madina.com/article/2</link>
            <pubDate>Sun, 02 Aug 2026 06:00:00 GMT</pubDate>
            <source url="https://www.al-madina.com">جريدة المدينة</source>
          </item>
        </channel></rss>
        """;

    [Fact]
    public void Parse_RealShapedFeed_ReturnsItemsInDocumentOrder()
    {
        IReadOnlyList<FetchedNewsItem> items = GoogleNewsRssParser.Parse(Feed);

        items.Should().HaveCount(2);
        items[0].Title.Should().Be("هيئة المنافسة تتلقى 112 طلب تركز اقتصادي في الربع الثاني 2026");
        items[0].SourceName.Should().Be("أرقام");
        items[0].SourceUrl.Should().Be("https://www.argaam.com/ar/article/1");
        items[0].ProviderKey.Should().Be(GoogleNewsRssProvider.Key);
    }

    [Fact]
    public void Parse_TitleWithDuplicatedOutletSuffix_StripsEveryOccurrence()
    {
        IReadOnlyList<FetchedNewsItem> items = GoogleNewsRssParser.Parse(Feed);

        items[1].Title.Should().Be("الهيئة العامة للمنافسة تُصدر 34 قراراً بعدم الممانعة");
    }

    [Fact]
    public void Parse_PubDate_IsReadAsUtcRegardlessOfHostCulture()
    {
        IReadOnlyList<FetchedNewsItem> items = GoogleNewsRssParser.Parse(Feed);

        items[0].PublishedAt.Should().Be(new DateTimeOffset(2026, 8, 3, 9, 30, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Parse_MalformedXml_ReturnsEmptyInsteadOfThrowing()
    {
        GoogleNewsRssParser.Parse("<rss><channel><item>truncated").Should().BeEmpty();
    }

    [Fact]
    public void Parse_EmptyPayload_ReturnsEmpty()
    {
        GoogleNewsRssParser.Parse(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Parse_ItemMissingLink_IsSkippedRatherThanStoredWithoutADedupKey()
    {
        const string feed = """
            <rss version="2.0"><channel>
              <item><title>عنوان بلا رابط</title></item>
            </channel></rss>
            """;

        GoogleNewsRssParser.Parse(feed).Should().BeEmpty();
    }

    [Fact]
    public void StripOutletSuffix_HeadlineContainingItsOwnDash_KeepsTheInternalDash()
    {
        var result = GoogleNewsRssParser.StripOutletSuffix("المنافسة - تقرير خاص - صحيفة مال", "صحيفة مال");

        result.Should().Be("المنافسة - تقرير خاص");
    }

    [Fact]
    public void StripOutletSuffix_TitleThatIsNothingButTheOutletName_KeepsTheOriginalTitle()
    {
        var result = GoogleNewsRssParser.StripOutletSuffix(" - أرقام", "أرقام");

        result.Should().Be("- أرقام");
    }

    [Fact]
    public void StripOutletSuffix_NoSourceName_ReturnsTitleUnchanged()
    {
        GoogleNewsRssParser.StripOutletSuffix("عنوان الخبر", string.Empty).Should().Be("عنوان الخبر");
    }

    [Fact]
    public void Parse_DescriptionOfLinkMarkup_YieldsPlainTextOrNullNotHtml()
    {
        IReadOnlyList<FetchedNewsItem> items = GoogleNewsRssParser.Parse(Feed);

        items[0].Body.Should().NotContain("<a href");
        items[1].Body.Should().BeNull();
    }
}
