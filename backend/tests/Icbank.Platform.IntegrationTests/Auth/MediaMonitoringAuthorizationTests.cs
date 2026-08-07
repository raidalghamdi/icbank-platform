using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Authorization-matrix coverage for the Wave 3a Media Monitoring + Prompts endpoints
/// (API-SURFACE.md §15): anonymous -> 401 (closes DEFECT-LOG.md SEC-02 -- 5 of these 11 routes
/// were completely unauthenticated in the Node source), viewer (zero grants) -> 403, super-admin
/// -> 200/201/404 as applicable.
/// </summary>
public sealed class MediaMonitoringAuthorizationTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListMediaReports_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/media-reports", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: this was an intentionally-public GET in the Node source but this port requires auth");
    }

    [Fact]
    public async Task GenerateMediaReport_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/media-reports/generate", UriKind.Relative), new { audience = "manager" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: unauthenticated AI-cost endpoint");
    }

    [Fact]
    public async Task GenerateMediaReport_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/media-reports/generate", UriKind.Relative), new { audience = "manager" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateMediaReport_SuperAdmin_ReturnsCreated()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/media-reports/generate", UriKind.Relative), new { audience = "manager", reportType = "weekly" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task CreatePrompt_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/prompts", UriKind.Relative), new { nameAr = "اسم", promptText = "نص" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: unauthenticated write to the shared prompt library");
    }

    [Fact]
    public async Task CreatePrompt_SuperAdmin_ReturnsCreated()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/prompts", UriKind.Relative), new { nameAr = "اسم القالب", promptText = "نص القالب" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task RunQuickAiTool_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/ai/quick", UriKind.Relative), new { tool = "summary", input = "نص" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: unauthenticated AI-cost endpoint");
    }

    [Fact]
    public async Task RunQuickAiTool_SuperAdmin_ReturnsOk()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/ai/quick", UriKind.Relative), new { tool = "summary", input = "نص طويل" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task GetMediaReportById_MissingId_ReturnsNotFound()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/media-reports/999999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePrompt_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync(new Uri("/api/v1/prompts/1", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
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
}
