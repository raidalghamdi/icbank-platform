using FluentAssertions;
using Icbank.Platform.Application.Common.Interfaces;
using Icbank.Platform.Infrastructure.InternationalDays;
using NSubstitute;
using Xunit;

namespace Icbank.Platform.UnitTests.Application.InternationalDays;

/// <summary>Verifies the ported 10-per-IP-per-hour rate limit (BUSINESS-RULES.md §4.1).</summary>
public sealed class InMemoryInternationalDaySearchRateLimiterTests
{
    private const string Ip = "203.0.113.5";
    private const int MaxSearchesPerWindow = 10;

    [Fact]
    public void TryConsume_UpToTenTimes_AllSucceedThenEleventhFails()
    {
        IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        var limiter = new InMemoryInternationalDaySearchRateLimiter(clock);

        for (var i = 0; i < MaxSearchesPerWindow; i++)
        {
            limiter.TryConsume(Ip).Should().BeTrue($"attempt {i + 1} is within the 10-per-hour limit");
        }

        limiter.TryConsume(Ip).Should().BeFalse("the 11th attempt within the same hour must be rejected");
    }

    [Fact]
    public void GetRemaining_AfterFiveConsumes_ReturnsFive()
    {
        IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        var limiter = new InMemoryInternationalDaySearchRateLimiter(clock);

        for (var i = 0; i < 5; i++)
        {
            limiter.TryConsume(Ip);
        }

        limiter.GetRemaining(Ip).Should().Be(5);
    }

    [Fact]
    public void TryConsume_AfterWindowExpires_ResetsCount()
    {
        IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();
        DateTimeOffset start = DateTimeOffset.UtcNow;
        clock.UtcNow.Returns(start);
        var limiter = new InMemoryInternationalDaySearchRateLimiter(clock);

        for (var i = 0; i < MaxSearchesPerWindow; i++)
        {
            limiter.TryConsume(Ip);
        }

        limiter.TryConsume(Ip).Should().BeFalse();

        clock.UtcNow.Returns(start.AddHours(1).AddSeconds(1));

        limiter.TryConsume(Ip).Should().BeTrue("the rolling window has fully elapsed, so the counter must reset");
    }

    [Fact]
    public void GetRemaining_UnknownIp_ReturnsFullQuota()
    {
        IDateTimeProvider clock = Substitute.For<IDateTimeProvider>();
        clock.UtcNow.Returns(DateTimeOffset.UtcNow);
        var limiter = new InMemoryInternationalDaySearchRateLimiter(clock);

        limiter.GetRemaining("198.51.100.9").Should().Be(MaxSearchesPerWindow);
    }
}
