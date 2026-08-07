using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Persistence;
using Icbank.Platform.IntegrationTests.Auth;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Api;

/// <summary>
/// Locks in the two cross-cutting response contracts that per-feature tests cannot enforce,
/// because each of those asserts only against the shape its own handler happens to return.
/// Both regressions covered here were found by the CI smoke test booting the API as a real
/// process, not by the in-process suite.
/// <para>
/// First: R-BE-033 mandates one pagination envelope for every collection endpoint, so a
/// generic client type can deserialise any list. Six endpoints had renamed
/// <c>items</c> to a feature-specific key (<c>issues</c>, <c>notifications</c>, <c>logs</c>,
/// <c>media</c>, <c>entries</c>, <c>drafts</c>), which silently broke that guarantee.
/// </para>
/// <para>
/// Second: R-BE-035 requires Problem Details on every error. Bare status codes produced by
/// the framework rather than a controller - an unmatched route, a wrong verb, an
/// unauthenticated call - were returning an empty body with no content type.
/// </para>
/// </summary>
public sealed class ResponseContractConventionTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private static readonly string[] RequiredEnvelopeKeys = { "items", "page", "pageSize", "total" };

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    /// <summary>Every paginated collection endpoint must answer with the identical envelope.</summary>
    /// <param name="route">The collection route under test.</param>
    /// <returns>A task representing the asynchronous test.</returns>
    [Theory]
    [InlineData("/api/v1/shorfah/issues")]
    [InlineData("/api/v1/notifications")]
    [InlineData("/api/v1/weekend/drafts")]
    [InlineData("/api/v1/week-start/archive")]
    public async Task CollectionEndpoint_ReturnsTheStandardPaginationEnvelope(string route)
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri(route, UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(body);

        foreach (var key in RequiredEnvelopeKeys)
        {
            document.RootElement.TryGetProperty(key, out _).Should().BeTrue(
                "R-BE-033 requires '{0}' on every collection response so one generic client type can read any list; {1} returned {2}",
                key,
                route,
                body);
        }

        document.RootElement.GetProperty("items").ValueKind.Should().Be(JsonValueKind.Array);
    }

    /// <summary>An unmatched route must still honour the Problem Details contract.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task UnmatchedRoute_ReturnsProblemDetails()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/no-such-route", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.Should().Be(
            "application/problem+json",
            "R-BE-035 admits no error response outside the Problem Details contract, including ones routing rejects before a controller is reached");
    }

    /// <summary>An unauthenticated call must also describe itself as Problem Details.</summary>
    /// <returns>A task representing the asynchronous test.</returns>
    [Fact]
    public async Task AnonymousCallToProtectedRoute_ReturnsProblemDetails()
    {
        HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/dashboard/summary", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/problem+json");
    }

    private async Task<HttpClient> ArrangeSuperAdminClientAsync()
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email = seeded.SuperAdmin.Email, password = SharedPassword });
        loginResponse.EnsureSuccessStatusCode();

        LoginResponsePayload? payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponsePayload>();
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        return client;
    }

    private AppDbContext CreateDbContext()
    {
        IServiceScope scope = _factory.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<AppDbContext>();
    }

    private sealed record LoginResponsePayload(string AccessToken);
}
