using FluentAssertions;
using Icbank.Platform.Application.MediaMonitoring;
using Icbank.Platform.Domain.Gac;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.MediaMonitoring;

/// <summary>Verifies <see cref="SourceFeedFormatter"/> ports <c>formatPostsForAI()</c> (BUSINESS-RULES.md §5.1): a flat numbered text block, chronologically ordered.</summary>
public sealed class SourceFeedFormatterTests
{
    [Fact]
    public void Format_EmptyInputs_ReturnsEmptyString()
    {
        var result = SourceFeedFormatter.Format(Array.Empty<GacSocialPost>(), Array.Empty<GacNewsItem>());

        result.Should().BeEmpty();
    }

    [Fact]
    public void Format_PostsAndNews_OrdersChronologicallyAndNumbers()
    {
        var earlyNews = new GacNewsItem { TitleAr = "خبر مبكر", PublishedAt = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero) };
        var latePost = new GacSocialPost { ContentAr = "منشور متأخر", PostUrl = "https://x.example/1", PostedAt = new DateTimeOffset(2026, 1, 2, 0, 0, 0, TimeSpan.Zero) };

        var result = SourceFeedFormatter.Format(new[] { latePost }, new[] { earlyNews });

        var lines = result.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        lines.Should().HaveCount(2);
        lines[0].Should().StartWith("1.").And.Contain("خبر مبكر");
        lines[1].Should().StartWith("2.").And.Contain("منشور متأخر");
    }

    [Fact]
    public void Format_PostMissingPostedAt_FallsBackToCreatedAt()
    {
        var post = new GacSocialPost { ContentAr = "بدون تاريخ نشر", PostUrl = "https://x.example/2", PostedAt = null, CreatedAt = DateTime.UtcNow };

        var result = SourceFeedFormatter.Format(new[] { post }, Array.Empty<GacNewsItem>());

        result.Should().Contain("بدون تاريخ نشر");
    }
}
