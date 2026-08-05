using FluentAssertions;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Infrastructure.Security;
using Xunit;

namespace Icbank.Platform.UnitTests.Infrastructure.Security;

/// <summary>
/// Verifies <see cref="GanssHtmlSanitizer"/> closes SEC-11 against real XSS payload shapes
/// (script tags, event-handler attributes, javascript:/data: URLs, SVG-based vectors) while
/// leaving benign Arabic rich-text formatting untouched.
/// </summary>
public sealed class GanssHtmlSanitizerTests
{
    private readonly GanssHtmlSanitizer _sanitizer = new();

    [Fact]
    public void Sanitize_ScriptTag_IsStrippedAndReportedAsModified()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize("<p>مرحبا</p><script>alert('xss')</script>");

        result.SanitizedHtml.Should().NotContain("<script", "the tag itself must be removed, not merely its content encoded");
        result.SanitizedHtml.Should().NotContain("alert");
        result.SanitizedHtml.Should().Contain("مرحبا");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_ImgOnErrorAttribute_StripsEventHandler()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize("<img src=x onerror=\"alert(1)\">");

        result.SanitizedHtml.Should().NotContain("onerror");
        result.SanitizedHtml.Should().NotContain("alert");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_AnchorOnMouseOverAttribute_StripsEventHandler()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize("<a href=\"https://example.com\" onmouseover=\"alert(1)\">link</a>");

        result.SanitizedHtml.Should().NotContain("onmouseover");
        result.SanitizedHtml.Should().Contain("href=\"https://example.com\"");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_JavascriptHrefScheme_IsRemoved()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize("<a href=\"javascript:alert(document.cookie)\">click</a>");

        result.SanitizedHtml.Should().NotContain("javascript:");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_JavascriptHrefWithWhitespaceAndCaseEvasion_IsStillRemoved()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize("<a href=\"  JaVaScRiPt&#58;alert(1)\">click</a>");

        result.SanitizedHtml.Should().NotContain("alert(1)");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_DataUriHref_IsRemoved()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize(
            "<a href=\"data:text/html;base64,PHNjcmlwdD5hbGVydCgxKTwvc2NyaXB0Pg==\">click</a>");

        result.SanitizedHtml.Should().NotContain("data:");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_SvgOnLoadVector_IsStripped()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize("<svg onload=\"alert(1)\"><circle r=\"1\"/></svg>");

        result.SanitizedHtml.Should().NotContain("<svg");
        result.SanitizedHtml.Should().NotContain("onload");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_SvgWithEmbeddedScript_IsStripped()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize("<svg><script>alert(1)</script></svg>");

        result.SanitizedHtml.Should().NotContain("<svg");
        result.SanitizedHtml.Should().NotContain("<script");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_IframeTag_IsStripped()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize("<p>قبل</p><iframe src=\"https://evil.example\"></iframe><p>بعد</p>");

        result.SanitizedHtml.Should().NotContain("<iframe");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_ObjectAndEmbedTags_AreStripped()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize(
            "<object data=\"https://evil.example/x.swf\"></object><embed src=\"https://evil.example/x.swf\">");

        result.SanitizedHtml.Should().NotContain("<object");
        result.SanitizedHtml.Should().NotContain("<embed");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_FormTag_IsStripped()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize("<form action=\"https://evil.example\"><input name=\"x\"></form>");

        result.SanitizedHtml.Should().NotContain("<form");
        result.SanitizedHtml.Should().NotContain("<input");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_StyleTagWithExpression_IsStripped()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize("<style>body { background: url('javascript:alert(1)'); }</style><p>محتوى</p>");

        result.SanitizedHtml.Should().NotContain("<style");
        result.SanitizedHtml.Should().Contain("محتوى");
        result.WasModified.Should().BeTrue();
    }

    [Fact]
    public void Sanitize_InlineStyleUrlVector_IsStripped()
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize(
            "<div style=\"background-image:url('https://evil.example/track.png')\">نص</div>");

        result.SanitizedHtml.Should().NotContain("url(");
        result.WasModified.Should().BeTrue();
    }

    [Theory]
    [InlineData("<p>مرحبا بكم في تقرير <strong>شرفة</strong> لهذا الأسبوع.</p>")]
    [InlineData("<h2>العنوان الرئيسي</h2><p>فقرة تحتوي على <em>تأكيد</em> و<u>خط تحت</u>.</p>")]
    [InlineData("<ul><li>بند أول</li><li>بند ثانٍ</li></ul>")]
    [InlineData("<p dir=\"rtl\" lang=\"ar\">نص عربي منسق بشكل صحيح.</p>")]
    [InlineData("<table><thead><tr><th>العمود</th></tr></thead><tbody><tr><td>قيمة</td></tr></tbody></table>")]
    [InlineData("<blockquote>اقتباس مهم</blockquote>")]
    [InlineData("<p>راجع <a href=\"https://example.com/report\">التقرير الكامل</a> للمزيد.</p>")]
    public void Sanitize_BenignArabicRichText_SurvivesUnchanged(string html)
    {
        HtmlSanitizationResult result = _sanitizer.Sanitize(html);

        result.SanitizedHtml.Should().Be(html);
        result.WasModified.Should().BeFalse();
    }

    [Fact]
    public void Sanitize_PlainArabicTextWithNoMarkup_IsNotFlaggedAsModified()
    {
        const string html = "هذا نص عادي بدون أي وسوم HTML على الإطلاق.";

        HtmlSanitizationResult result = _sanitizer.Sanitize(html);

        result.SanitizedHtml.Should().Be(html);
        result.WasModified.Should().BeFalse();
    }
}
