using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Authorization-matrix coverage for the Wave 1 endpoints (Health/Storage/Dashboard/Daily
/// Report/Week Start/Weekend Places/Weekend Drafts): anonymous -> 401/liveness-only, viewer
/// (zero grants) -> 403, super-admin -> 200/201 (task requirement: "each endpoint needs at least
/// an authorization test"). Uses the existing <see cref="AuthWebApplicationFactory"/>/
/// <see cref="AuthTestDataBuilder"/> fixture — <c>super_admin</c> bypasses every generated
/// policy per <c>PermissionAuthorizationHandler</c>, so it stands in for "correctly permissioned"
/// without needing to seed the new page slugs' role_permissions rows for every test.
/// </summary>
public sealed class CoreEndpointAuthorizationTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Healthz_NoToken_ReturnsOk()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/healthz", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "liveness must remain anonymous per task instruction");
    }

    [Fact]
    public async Task StorageObjects_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/storage/objects/gac/publications/x.pdf", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes the Node source's effectively-always-public storage route");
    }

    [Fact]
    public async Task StorageObjects_DisallowedPrefix_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/storage/objects/etc/passwd", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DashboardSummary_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/dashboard/summary", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DashboardSummary_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/dashboard/summary", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DashboardSummary_SuperAdmin_ReturnsOk()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/dashboard/summary", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DashboardAiSummary_SuperAdmin_ReturnsGeneratedSummary()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/dashboard/ai-summary", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task DailyReportUpsert_NoApiKey_ReturnsForbidden()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/daily-report", UriKind.Relative), new { reportDate = "2026-08-05", reportData = new { } });

        new[] { HttpStatusCode.Unauthorized, HttpStatusCode.Forbidden }.Should().Contain(response.StatusCode, "the cron-api-key policy must reject requests without a valid key");
    }

    [Fact]
    public async Task DailyReportLatest_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/daily-report/latest", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes AMBIGUOUS-API-1 — the Node source left this effectively public due to router mount ordering");
    }

    [Fact]
    public async Task DailyReportLatest_SuperAdminNoReportsYet_ReturnsNotFound()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/daily-report/latest", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Wk2Data_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/wk2-data", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Wk2Data_AuthenticatedViewer_ReturnsOk()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/wk2-data", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "wk2-data is gated only by the blanket authenticated check, matching the Node source's intent");
    }

    [Fact]
    public async Task WeekendPlacesList_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/weekend-places", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WeekendPlacesCreate_SuperAdmin_ReturnsCreated()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/weekend-places", UriKind.Relative),
            new { name = "منتزه الملك عبدالله", description = "منتزه عائلي", sortOrder = 1 });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task WeekendDraftsGenerate_SuperAdmin_ReturnsCreatedPendingReviewDraft()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(new Uri("/api/v1/weekend/generate", UriKind.Relative), new { });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task WeekendDraftsPublished_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/weekend/published", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task WeekendDraftsList_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/weekend/drafts", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WeekendSend_SuperAdminWithChannels_ReturnsHonestNotImplementedResult()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/weekend/send", UriKind.Relative),
            new { channels = new[] { new { type = "email", to = "someone@example.com" } } });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Contain("not_implemented", "BUG-01 fix: no channel may claim a fabricated queued/success status");
    }

    [Fact]
    public async Task WeekStartArchive_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/week-start/archive", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task WeekStartGenerate_SuperAdmin_ReturnsThreeModelOutputs()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/week-start/generate", UriKind.Relative), new { topic = "الابتكار" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WeekStartStyleProfile_SuperAdminNoProfileYet_ReturnsOkWithNullBody()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/week-start/style-profile", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "ASP.NET Core serializes a null ActionResult<T> body as 204 No Content, matching the Node source's 'profile ?? null' intent");
    }

    [Fact]
    public async Task WeekStartOutputs_SuperAdmin_ReturnsOk()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/week-start/outputs", UriKind.Relative));

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
}
