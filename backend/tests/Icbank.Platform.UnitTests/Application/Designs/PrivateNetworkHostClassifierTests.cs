using FluentAssertions;
using Icbank.Platform.Application.Designs.IconEvent.Commands;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.Designs;

/// <summary>Verifies <see cref="PrivateNetworkHostClassifier"/> catches every address-space vector named by SEC-12, in both literal-IP and DNS-name forms.</summary>
public sealed class PrivateNetworkHostClassifierTests
{
    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("127.255.255.255")]
    [InlineData("10.0.0.1")]
    [InlineData("10.255.255.255")]
    [InlineData("172.16.0.1")]
    [InlineData("172.31.255.255")]
    [InlineData("192.168.0.1")]
    [InlineData("192.168.255.255")]
    [InlineData("169.254.0.1")]
    [InlineData("169.254.169.254")] // cloud metadata endpoint
    [InlineData("::1")]
    [InlineData("fc00::1")]
    [InlineData("fd12:3456:789a::1")]
    public void IsPrivateOrLinkLocal_KnownPrivateOrLinkLocalLiteral_ReturnsTrue(string host)
    {
        PrivateNetworkHostClassifier.IsPrivateOrLinkLocal(host).Should().BeTrue();
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("LOCALHOST")]
    [InlineData("localhost.localdomain")]
    [InlineData("metadata.google.internal")]
    [InlineData("metadata")]
    [InlineData("something.localhost")]
    [InlineData("printer.local")]
    public void IsPrivateOrLinkLocal_LocalhostStyleDnsName_ReturnsTrue(string host)
    {
        PrivateNetworkHostClassifier.IsPrivateOrLinkLocal(host).Should().BeTrue();
    }

    [Theory]
    [InlineData("172.15.255.255")] // just below 172.16.0.0/12
    [InlineData("172.32.0.0")] // just above 172.16.0.0/12
    [InlineData("8.8.8.8")]
    [InlineData("93.184.216.34")]
    [InlineData("2606:4700:4700::1111")] // public IPv6 (Cloudflare)
    [InlineData("example.com")]
    [InlineData("cdn.example.com")]
    public void IsPrivateOrLinkLocal_PublicAddressOrHostname_ReturnsFalse(string host)
    {
        PrivateNetworkHostClassifier.IsPrivateOrLinkLocal(host).Should().BeFalse();
    }

    [Fact]
    public void IsPrivateOrLinkLocal_EmptyOrWhitespace_ReturnsFalse()
    {
        PrivateNetworkHostClassifier.IsPrivateOrLinkLocal(string.Empty).Should().BeFalse();
        PrivateNetworkHostClassifier.IsPrivateOrLinkLocal("   ").Should().BeFalse();
    }
}
