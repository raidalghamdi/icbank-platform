using System.Net;
using FluentAssertions;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Api;

/// <summary>
/// End-to-end pipeline proof (R-BE-081). Deliberately exercises only <c>/health/live</c> and the
/// versioned ping endpoint — neither touches <c>AppDbContext</c> — so this suite passes without a
/// live SQL Server, per the task's guard requirement.
/// </summary>
public sealed class HealthAndPingEndpointTests : IClassFixture<IcbankWebApplicationFactory>
{
    private readonly IcbankWebApplicationFactory _factory;

    public HealthAndPingEndpointTests(IcbankWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealthLive_ReturnsOk()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/health/live", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetApiV1Ping_ReturnsOkWithPongMessage()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/ping", UriKind.Relative));
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        body.Should().Contain("pong");
    }

    [Fact]
    public async Task GetApiV1Ping_ResponseIncludesCorrelationIdHeader()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/ping", UriKind.Relative));

        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
    }
}
