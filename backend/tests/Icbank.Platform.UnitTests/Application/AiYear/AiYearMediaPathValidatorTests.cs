using FluentAssertions;
using Icbank.Platform.Application.AiYear;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.AiYear;

/// <summary>Verifies the ported <c>SAFE_OBJECT_PATH</c> regex (BUSINESS-RULES.md §3).</summary>
public sealed class AiYearMediaPathValidatorTests
{
    [Theory]
    [InlineData("/objects/ai-year/2026/1/42/photo.jpg")]
    [InlineData("/objects/ai-year/2026/12/1/video.mp4")]
    [InlineData("/objects/ai-year/2026/9/999/file-name.with.dots.png")]
    public void IsValid_WellFormedPath_ReturnsTrue(string path)
    {
        AiYearMediaPathValidator.IsValid(path).Should().BeTrue();
    }

    [Theory]
    [InlineData("/objects/ai-year/2026/0/42/photo.jpg")]
    [InlineData("/objects/ai-year/2026/13/42/photo.jpg")]
    [InlineData("/objects/ai-year/2025/1/42/photo.jpg")]
    [InlineData("/objects/ai-year/2026/1/abc/photo.jpg")]
    [InlineData("/objects/ai-year/2026/1/42/../../../etc/passwd")]
    [InlineData("/objects/other/2026/1/42/photo.jpg")]
    [InlineData("photo.jpg")]
    public void IsValid_MalformedOrTraversalPath_ReturnsFalse(string path)
    {
        AiYearMediaPathValidator.IsValid(path).Should().BeFalse();
    }
}
