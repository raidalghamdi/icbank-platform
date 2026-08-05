using FluentAssertions;
using Icbank.Platform.Application.Designs.IconEvent.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs;

/// <summary>
/// Verifies <see cref="HtmlRemoteResourceScanner"/> flags every SSRF vector named by SEC-12: remote
/// <c>img</c>/<c>script</c>/<c>link</c>/<c>iframe</c>/<c>video</c>/<c>audio</c>/<c>source</c>/
/// <c>object</c>/<c>embed</c> references, inline-style <c>url()</c>, <c>@import</c>, and SVG
/// <c>xlink:href</c> -- across public hosts, every required private/link-local range (including
/// the metadata endpoint), and DNS-name forms, while leaving purely local/relative/data content
/// alone.
/// </summary>
public sealed class HtmlRemoteResourceScannerTests
{
    [Theory]
    [InlineData("<img src=\"https://evil.example/pixel.png\">")]
    [InlineData("<script src=\"https://evil.example/x.js\"></script>")]
    [InlineData("<link rel=\"stylesheet\" href=\"https://evil.example/x.css\">")]
    [InlineData("<iframe src=\"https://evil.example/\"></iframe>")]
    [InlineData("<video src=\"https://evil.example/x.mp4\"></video>")]
    [InlineData("<audio src=\"https://evil.example/x.mp3\"></audio>")]
    [InlineData("<video><source src=\"https://evil.example/x.mp4\"></video>")]
    [InlineData("<object data=\"https://evil.example/x.swf\"></object>")]
    [InlineData("<embed src=\"https://evil.example/x.swf\">")]
    [InlineData("<div style=\"background:url('https://evil.example/track.png')\"></div>")]
    [InlineData("<style>@import \"https://evil.example/x.css\";</style>")]
    [InlineData("<style>body{background:url(https://evil.example/x.png)}</style>")]
    [InlineData("<svg><image xlink:href=\"https://evil.example/x.png\"/></svg>")]
    [InlineData("<svg><use href=\"https://evil.example/sprite.svg#icon\"/></svg>")]
    public void FindRemoteReferences_EachSsrfVectorShape_IsDetected(string html)
    {
        HtmlRemoteResourceScanner.FindRemoteReferences(html).Should().NotBeEmpty();
    }

    [Theory]
    [InlineData("<img src=\"http://127.0.0.1/x\">")]
    [InlineData("<img src=\"http://169.254.169.254/latest/meta-data/\">")] // cloud metadata endpoint, literal IP
    [InlineData("<img src=\"http://[::1]/x\">")]
    [InlineData("<img src=\"http://[fc00::1]/x\">")]
    [InlineData("<img src=\"http://10.0.0.5/x\">")]
    [InlineData("<img src=\"http://172.16.0.1/x\">")]
    [InlineData("<img src=\"http://192.168.1.1/x\">")]
    public void FindRemoteReferences_PrivateOrLinkLocalLiteralIp_IsDetectedAndClassifiedAsPrivate(string html)
    {
        HtmlRemoteResourceScanner.FindRemoteReferences(html).Should().NotBeEmpty();

        IReadOnlyList<(string Url, bool TargetsPrivateOrLinkLocalAddressSpace)> classified = HtmlRemoteResourceScanner.ClassifyRemoteReferences(html);
        classified.Should().ContainSingle(entry => entry.TargetsPrivateOrLinkLocalAddressSpace);
    }

    [Theory]
    [InlineData("<img src=\"http://localhost/x\">")]
    [InlineData("<img src=\"http://localhost:8080/x\">")]
    [InlineData("<img src=\"http://metadata.google.internal/computeMetadata/v1/\">")] // metadata endpoint, DNS-name form
    [InlineData("<script src=\"//localhost/x.js\"></script>")]
    public void FindRemoteReferences_LocalhostStyleDnsNameForm_IsDetectedAndClassifiedAsPrivate(string html)
    {
        HtmlRemoteResourceScanner.FindRemoteReferences(html).Should().NotBeEmpty();

        IReadOnlyList<(string Url, bool TargetsPrivateOrLinkLocalAddressSpace)> classified = HtmlRemoteResourceScanner.ClassifyRemoteReferences(html);
        classified.Should().ContainSingle(entry => entry.TargetsPrivateOrLinkLocalAddressSpace);
    }

    [Fact]
    public void FindRemoteReferences_PublicHttpsReference_IsDetectedButNotClassifiedAsPrivate()
    {
        const string html = "<img src=\"https://cdn.example.com/logo.png\">";

        HtmlRemoteResourceScanner.FindRemoteReferences(html).Should().NotBeEmpty();

        IReadOnlyList<(string Url, bool TargetsPrivateOrLinkLocalAddressSpace)> classified = HtmlRemoteResourceScanner.ClassifyRemoteReferences(html);
        classified.Should().ContainSingle(entry => !entry.TargetsPrivateOrLinkLocalAddressSpace);
    }

    [Theory]
    [InlineData("<p>مرحبا بكم في التقرير</p>")]
    [InlineData("<img src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA\">")]
    [InlineData("<a href=\"#section-2\">القسم الثاني</a>")]
    [InlineData("<div style=\"color:red;font-weight:bold\">نص</div>")]
    [InlineData("<img src=\"assets/local-logo.png\">")]
    public void FindRemoteReferences_BenignOrLocalContent_IsNotFlagged(string html)
    {
        HtmlRemoteResourceScanner.FindRemoteReferences(html).Should().BeEmpty();
    }

    [Fact]
    public void FindRemoteReferences_SchemeRelativeUrl_IsDetected()
    {
        const string html = "<script src=\"//evil.example/x.js\"></script>";

        HtmlRemoteResourceScanner.FindRemoteReferences(html).Should().ContainSingle();
    }

    [Fact]
    public void FindRemoteReferences_MultipleDistinctReferences_AreAllReturned()
    {
        const string html = "<img src=\"https://a.example/1.png\"><script src=\"https://b.example/2.js\"></script>";

        HtmlRemoteResourceScanner.FindRemoteReferences(html).Should().HaveCount(2);
    }

    [Fact]
    public void FindRemoteReferences_DuplicateReference_IsDeduplicated()
    {
        const string html = "<img src=\"https://a.example/1.png\"><img src=\"https://a.example/1.png\">";

        HtmlRemoteResourceScanner.FindRemoteReferences(html).Should().ContainSingle();
    }
}
