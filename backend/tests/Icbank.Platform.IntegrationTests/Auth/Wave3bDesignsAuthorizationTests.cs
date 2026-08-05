using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Authorization-matrix coverage for Wave 3b's Designs/Composer endpoints (API-SURFACE.md §17).
/// The Node source gated the entire router with a blanket <c>requireAdmin</c> -- this port
/// requires <c>design_studio:{verb}</c>: anonymous -> 401, super-admin -> 200/201/404 as
/// applicable.
/// </summary>
public sealed class Wave3bDesignsAuthorizationTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListTemplates_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/designs/templates", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListTemplates_SuperAdmin_ReturnsOk()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/designs/templates", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task CreateTemplate_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/templates", UriKind.Relative), new { templateNameAr = "قالب", category = "general", canvasWidth = 1920, canvasHeight = 1080 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CreateTemplate_SuperAdmin_ReturnsCreated()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/templates", UriKind.Relative), new { templateNameAr = "قالب جديد", category = "general", canvasWidth = 1920, canvasHeight = 1080 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task ReseedPresentation_SuperAdmin_ReturnsOkAndIsIdempotentOnSecondCall()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage first = await client.PostAsync(new Uri("/api/v1/designs/templates/reseed-presentation", UriKind.Relative), content: null);
        HttpResponseMessage second = await client.PostAsync(new Uri("/api/v1/designs/templates/reseed-presentation", UriKind.Relative), content: null);

        first.StatusCode.Should().Be(HttpStatusCode.OK);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        ReseedPayload? secondPayload = await second.Content.ReadFromJsonAsync<ReseedPayload>();
        secondPayload!.Notes.Should().NotBeEmpty("the second call must update existing rows by name, not duplicate them");
    }

    [Fact]
    public async Task GenerateBackgrounds_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/generate-backgrounds", UriKind.Relative), new { prompt = "منظر جبلي" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "external-cost AI endpoint must require an explicit policy grant");
    }

    [Fact]
    public async Task GenerateBackgrounds_SuperAdmin_ReturnsOkWithFourImages()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/designs/generate-backgrounds", UriKind.Relative), new { prompt = "منظر جبلي هادئ" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        GenerateBackgroundsPayload? payload = await response.Content.ReadFromJsonAsync<GenerateBackgroundsPayload>();
        payload!.Images.Should().HaveCount(4);
    }

    [Fact]
    public async Task DeleteTemplate_MissingId_ReturnsNotFound()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.DeleteAsync(new Uri("/api/v1/designs/templates/999999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
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

    private sealed record ReseedPayload(int Count, IReadOnlyList<string> Notes);

    private sealed record GenerateBackgroundsPayload(IReadOnlyList<object> Images);
}
