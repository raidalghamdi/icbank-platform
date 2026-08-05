using FluentAssertions;
using Icbank.Platform.Application.Shorfah;
using Icbank.Platform.Domain.Shorfah;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>
/// Proves <see cref="ShorfahIssueHtmlBuilder"/> HTML-encodes every interpolation site --
/// closing SEC-04/H-1 (BUSINESS-RULES.md §1.9's flagged stored-XSS risk in the Node source's
/// <c>shorfah-pdf.ts</c>) for the .NET port.
/// </summary>
public sealed class ShorfahIssueHtmlBuilderTests
{
    [Fact]
    public void Build_TitleContainingScriptTag_IsHtmlEncodedNotRawInjected()
    {
        var issue = new ShorfahIssue { TitleAr = "<script>alert(1)</script>", Month = 8, Year = 2026 };

        var html = ShorfahIssueHtmlBuilder.Build(issue, new List<ShorfahSection>());

        html.Should().NotContain("<script>alert(1)</script>");
        html.Should().Contain("&lt;script&gt;");
    }

    [Fact]
    public void Build_SectionContentContainingScriptTag_IsHtmlEncoded()
    {
        var issue = new ShorfahIssue { TitleAr = "عدد", Month = 8, Year = 2026 };
        var section = new ShorfahSection { TitleAr = "قسم", ContentMd = "<img src=x onerror=alert(1)>" };

        var html = ShorfahIssueHtmlBuilder.Build(issue, new List<ShorfahSection> { section });

        html.Should().NotContain("<img src=x onerror=alert(1)>");
        html.Should().Contain("&lt;img");
    }

    [Fact]
    public void Build_IncludesEditorLetterWhenPresent()
    {
        var issue = new ShorfahIssue { TitleAr = "عدد", Month = 8, Year = 2026, EditorLetter = "رسالة تحرير" };

        var html = ShorfahIssueHtmlBuilder.Build(issue, new List<ShorfahSection>());

        html.Should().Contain("رسالة رئيس التحرير");
        html.Should().Contain("رسالة تحرير");
    }

    [Fact]
    public void Build_OmitsEditorLetterSectionWhenAbsent()
    {
        var issue = new ShorfahIssue { TitleAr = "عدد", Month = 8, Year = 2026 };

        var html = ShorfahIssueHtmlBuilder.Build(issue, new List<ShorfahSection>());

        html.Should().NotContain("رسالة رئيس التحرير");
    }
}
