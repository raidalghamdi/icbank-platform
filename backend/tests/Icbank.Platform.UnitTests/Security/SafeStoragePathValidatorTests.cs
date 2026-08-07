using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Infrastructure.Security;
using Xunit;

namespace Icbank.Platform.UnitTests.Security;

/// <summary>
/// Unit tests for <see cref="SafeStoragePathValidator"/> (task requirement 3/4: "hardened,
/// reusable path/filename validation utility... unit-tested against traversal payloads (../,
/// encoded variants, absolute paths, null bytes, UNC)"), closing SEC-17.
/// </summary>
public sealed class SafeStoragePathValidatorTests
{
    private static readonly string[] ShorfahPrefix = { "shorfah/" };
    private readonly SafeStoragePathValidator _validator = new();

    [Theory]
    [InlineData("../../etc/passwd")]
    [InlineData("shorfah/sections/../../../etc/passwd")]
    [InlineData("..\\..\\windows\\win.ini")]
    [InlineData("shorfah/../../secrets.txt")]
    public void Validate_LiteralTraversalSegment_IsRejected(string candidate)
    {
        SafePathValidationResult result = _validator.Validate(candidate, ShorfahPrefix);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("shorfah%2f..%2f..%2fetc%2fpasswd")]
    [InlineData("shorfah/sections/%2e%2e/%2e%2e/etc/passwd")]
    [InlineData("shorfah/sections/%252e%252e/%252e%252e/etc/passwd")]
    [InlineData("shorfah/%2e%2e%2f%2e%2e%2fetc%2fpasswd")]
    public void Validate_EncodedTraversalVariant_IsRejected(string candidate)
    {
        SafePathValidationResult result = _validator.Validate(candidate, ShorfahPrefix);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Windows/System32/config/sam")]
    [InlineData("c:\\Windows\\System32")]
    public void Validate_AbsolutePath_IsRejected(string candidate)
    {
        SafePathValidationResult result = _validator.Validate(candidate, ShorfahPrefix);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("shorfah/sections/file\0.png")]
    [InlineData("shorfah/\0/passwd")]
    public void Validate_NullByte_IsRejected(string candidate)
    {
        SafePathValidationResult result = _validator.Validate(candidate, ShorfahPrefix);

        result.IsValid.Should().BeFalse();
    }

    [Theory]
    [InlineData("\\\\attacker-host\\share\\file.txt")]
    [InlineData("//attacker-host/share/file.txt")]
    public void Validate_UncPath_IsRejected(string candidate)
    {
        SafePathValidationResult result = _validator.Validate(candidate, ShorfahPrefix);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WellFormedPathWithinAllowedPrefix_IsAccepted()
    {
        SafePathValidationResult result = _validator.Validate("shorfah/sections/42/photo.png", ShorfahPrefix);

        result.IsValid.Should().BeTrue();
        result.NormalizedPath.Should().Be("shorfah/sections/42/photo.png");
    }

    [Fact]
    public void Validate_WellFormedPathOutsideAllowedPrefix_IsRejected()
    {
        SafePathValidationResult result = _validator.Validate("gac/publications/report.pdf", ShorfahPrefix);

        result.IsValid.Should().BeFalse();
        result.RejectionReason.Should().Be("prefix_not_allowed");
    }

    [Fact]
    public void Validate_EmptyPath_IsRejected()
    {
        SafePathValidationResult result = _validator.Validate(string.Empty, ShorfahPrefix);

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_NoAllowedPrefixesRequired_StillBlocksTraversal()
    {
        SafePathValidationResult result = _validator.Validate("../../etc/passwd", Array.Empty<string>());

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_DotSegmentThatStaysWithinRoot_IsAccepted()
    {
        // "shorfah/sections/./42/../43/photo.png" normalizes to "shorfah/sections/43/photo.png"
        // and never escapes the virtual root, so this is legitimate input, not an attack.
        SafePathValidationResult result = _validator.Validate("shorfah/sections/./42/../43/photo.png", ShorfahPrefix);

        result.IsValid.Should().BeTrue();
        result.NormalizedPath.Should().Be("shorfah/sections/43/photo.png");
    }
}
