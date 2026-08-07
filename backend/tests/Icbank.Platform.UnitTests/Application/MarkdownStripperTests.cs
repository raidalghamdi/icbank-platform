using FluentAssertions;
using Icbank.Platform.Application.Shorfah;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>Proves <see cref="MarkdownStripper"/> ports <c>stripMd()</c> (shorfah.ts:1130-1138) exactly.</summary>
public sealed class MarkdownStripperTests
{
    [Fact]
    public void Strip_NullOrEmpty_ReturnsEmptyString()
    {
        MarkdownStripper.Strip(null).Should().BeEmpty();
        MarkdownStripper.Strip(string.Empty).Should().BeEmpty();
    }

    [Fact]
    public void Strip_RemovesImageSyntax()
    {
        MarkdownStripper.Strip("before ![alt](http://x/y.png) after").Should().Be("before  after");
    }

    [Fact]
    public void Strip_KeepsLinkTextButRemovesUrl()
    {
        MarkdownStripper.Strip("see [النص](http://example.com) هنا").Should().Be("see النص هنا");
    }

    [Fact]
    public void Strip_RemovesEmphasisAndHeadingCharacters()
    {
        MarkdownStripper.Strip("# عنوان **مهم** _مائل_ `كود` > اقتباس").Should().Be("عنوان مهم مائل كود  اقتباس");
    }

    [Fact]
    public void Strip_CollapsesExcessBlankLines()
    {
        MarkdownStripper.Strip("سطر1\n\n\n\nسطر2").Should().Be("سطر1\n\nسطر2");
    }

    [Fact]
    public void Strip_TrimsLeadingAndTrailingWhitespace()
    {
        MarkdownStripper.Strip("   محتوى   ").Should().Be("محتوى");
    }
}
