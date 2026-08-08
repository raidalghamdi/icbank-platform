using FluentAssertions;
using FluentValidation.Results;
using Icbank.Platform.Application.Designs.IconEvent.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs.IconEvent;

/// <summary>
/// Verifies <see cref="RenderIconEventDesignCommandValidator"/> rejects every SSRF vector named by
/// SEC-12 at the input boundary -- closing the half of SEC-12 left open because the placeholder
/// renderer never fetches anything -- while still accepting benign, fully local HTML.
/// </summary>
public sealed class RenderIconEventDesignCommandValidatorTests
{
    private readonly RenderIconEventDesignCommandValidator _validator = new();

    [Theory]
    [InlineData("<img src=\"https://evil.example/pixel.png\">")]
    [InlineData("<script src=\"https://evil.example/x.js\"></script>")]
    [InlineData("<link rel=\"stylesheet\" href=\"https://evil.example/x.css\">")]
    [InlineData("<iframe src=\"https://evil.example/\"></iframe>")]
    [InlineData("<video src=\"https://evil.example/x.mp4\"></video>")]
    [InlineData("<audio src=\"https://evil.example/x.mp3\"></audio>")]
    [InlineData("<object data=\"https://evil.example/x.swf\"></object>")]
    [InlineData("<embed src=\"https://evil.example/x.swf\">")]
    [InlineData("<div style=\"background:url('https://evil.example/track.png')\"></div>")]
    [InlineData("<style>@import \"https://evil.example/x.css\";</style>")]
    [InlineData("<svg><image xlink:href=\"https://evil.example/x.png\"/></svg>")]
    public void Validate_RemoteResourceReference_Fails(string htmlSnippet)
    {
        RenderIconEventDesignCommand command = Build($"<div>{htmlSnippet}</div>");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == nameof(RenderIconEventDesignCommand.Html));
    }

    [Theory]
    [InlineData("http://127.0.0.1/x")]
    [InlineData("http://169.254.169.254/latest/meta-data/iam/security-credentials/")]
    [InlineData("http://10.0.0.5/x")]
    [InlineData("http://172.16.0.1/x")]
    [InlineData("http://192.168.1.1/x")]
    [InlineData("http://[::1]/x")]
    [InlineData("http://[fc00::1]/x")]
    public void Validate_PrivateOrLinkLocalLiteralIpReference_Fails(string url)
    {
        RenderIconEventDesignCommand command = Build($"<img src=\"{url}\">");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("http://localhost/x")]
    [InlineData("http://metadata.google.internal/computeMetadata/v1/")]
    public void Validate_LocalhostStyleDnsNameReference_Fails(string url)
    {
        RenderIconEventDesignCommand command = Build($"<img src=\"{url}\">");

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("<p>مرحبا بكم في هذا التصميم</p>")]
    [InlineData("<img src=\"data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAUA\">")]
    [InlineData("<img src=\"assets/local-logo.png\">")]
    [InlineData("<div style=\"color:red;font-weight:bold\">نص منسق</div>")]
    public void Validate_BenignOrLocalOnlyHtml_Passes(string html)
    {
        RenderIconEventDesignCommand command = Build(html);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_EmptyHtml_FailsWithoutInvokingRemoteResourceRule()
    {
        RenderIconEventDesignCommand command = Build(string.Empty);

        ValidationResult result = _validator.Validate(command);

        result.IsValid.Should().BeFalse();
    }

    private static RenderIconEventDesignCommand Build(string html) => new(1, html, "desktop-hd", "hd");
}
