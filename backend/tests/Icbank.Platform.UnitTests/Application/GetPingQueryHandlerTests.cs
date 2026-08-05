using FluentAssertions;
using Icbank.Platform.Application.Common.Models;
using Icbank.Platform.Application.Ping;
using Xunit;

namespace Icbank.Platform.UnitTests.Application;

/// <summary>Verifies <see cref="GetPingQueryHandler"/> returns the expected <see cref="Result{T}"/> shape.</summary>
public sealed class GetPingQueryHandlerTests
{
    [Fact]
    public async Task Handle_WhenInvoked_ReturnsSuccessfulPongResult()
    {
        GetPingQueryHandler handler = new();
        DateTime before = DateTime.UtcNow;

        Result<PingResponse> result = await handler.Handle(new GetPingQuery(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Message.Should().Be("pong");
        result.Value.ServerTimeUtc.Should().BeOnOrAfter(before);
    }
}
