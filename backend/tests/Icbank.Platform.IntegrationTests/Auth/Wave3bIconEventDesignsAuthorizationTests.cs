using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Authorization-matrix coverage for Wave 3b's Icon Event Designs endpoints
/// (API-SURFACE.md §18). The Node source gated this router with only <c>requireAuth</c>
/// (SEC-02 mandates every mutating route require an explicit policy) -- this port requires
/// <c>design_studio:{verb}</c>: anonymous -> 401, super-admin -> 200/429 as applicable.
/// </summary>
public sealed class Wave3bIconEventDesignsAuthorizationTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListIcons_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/designs/icon-event/icons", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListIcons_SuperAdmin_ReturnsOk()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/designs/icon-event/icons", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GenerateDesign_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/icon-event/generate", UriKind.Relative), new { headline = "عنوان تجريبي", size = "landscape" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: this route was requireAuth-only (no policy) in the Node source and is also an external-cost abuse vector");
    }

    [Fact]
    public async Task GenerateDesign_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/icon-event/generate", UriKind.Relative), new { headline = "عنوان تجريبي", size = "landscape" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateDesign_SuperAdmin_ReturnsOkWithThreeVariants()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/icon-event/generate", UriKind.Relative), new { headline = "ورشة عمل عن الابتكار", size = "landscape" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GenerateResponsePayload? payload = await response.Content.ReadFromJsonAsync<GenerateResponsePayload>();
        payload!.Count.Should().Be(3);
    }

    [Fact]
    public async Task GenerateDesign_InvalidSize_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/icon-event/generate", UriKind.Relative), new { headline = "عنوان تجريبي", size = "poster" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Studio_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/icon-event/studio", UriKind.Relative), new { headline = "عنوان" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Studio_SuperAdmin_ReturnsOk()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/icon-event/studio", UriKind.Relative), new { headline = "عنوان" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Render_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/icon-event/render", UriKind.Relative), new { html = "<html></html>", size = "landscape" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-12's authz half: the Node source allowed any requireAuth user; this port requires an explicit policy grant");
    }

    [Fact]
    public async Task Render_SuperAdmin_ReturnsOk()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/icon-event/render", UriKind.Relative), new { html = "<html><body>test</body></html>", size = "landscape" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    private async Task<HttpClient> ArrangeAuthenticatedClientAsync(bool useSuperAdmin = false, bool useViewer = false)
    {
        using AppDbContext dbContext = CreateDbContext();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);

        var email = useSuperAdmin ? seeded.SuperAdmin.Email : useViewer ? seeded.Viewer.Email : seeded.Admin.Email;

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email, password = SharedPassword });
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

    private sealed record GenerateResponsePayload(int Count);
}
