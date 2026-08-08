using FluentAssertions;
using Icbank.Platform.Application.Gac.News;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Gac;

/// <summary>Tests for <see cref="NewsBodySanitizer"/>.</summary>
public sealed class NewsBodySanitizerTests
{
    private const string Title = "هيئة المنافسة تُصدر 34 قراراً بخصوص طلبات التركز الاقتصادي";

    [Fact]
    public void Sanitize_BodyRepeatingTheTitleWithTheOutletAppended_IsDropped()
    {
        // The exact shape Google News RSS returns.
        NewsBodySanitizer.Sanitize(Title, Title + " ارقام", "ارقام").Should().BeNull();
    }

    [Fact]
    public void Sanitize_BodyIdenticalToTheTitle_IsDropped()
    {
        NewsBodySanitizer.Sanitize(Title, Title, null).Should().BeNull();
    }

    [Fact]
    public void Sanitize_BodyDifferingOnlyByPunctuationAndSpacing_IsDropped()
    {
        NewsBodySanitizer.Sanitize(Title, "  " + Title.Replace("34", "34،") + "...  ", null)
            .Should().BeNull();
    }

    [Fact]
    public void Sanitize_RealSummary_IsKept()
    {
        const string summary =
            "أوضحت الهيئة أن القرارات شملت طلبات تركز اقتصادي في قطاعات الصحة والتجزئة، "
            + "وأن مدة دراسة الطلب لم تتجاوز 30 يوماً.";

        NewsBodySanitizer.Sanitize(Title, summary, "ارقام").Should().Be(summary);
    }

    [Fact]
    public void Sanitize_TitleFollowedByASubstantialContinuation_IsKept()
    {
        var body = Title + " وأضافت الهيئة في بيان صحفي أن المراجعة استغرقت ثلاثين يوماً وشملت عدة قطاعات حيوية.";

        NewsBodySanitizer.Sanitize(Title, body, null).Should().Be(body);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Sanitize_EmptyBody_IsNull(string? body)
    {
        NewsBodySanitizer.Sanitize(Title, body, "ارقام").Should().BeNull();
    }

    [Fact]
    public void Sanitize_BodyOfOnlyPunctuation_IsDropped()
    {
        NewsBodySanitizer.Sanitize(Title, "-- ... --", null).Should().BeNull();
    }
}
