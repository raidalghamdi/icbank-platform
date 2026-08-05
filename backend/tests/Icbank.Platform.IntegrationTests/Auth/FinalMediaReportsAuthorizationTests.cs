using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Authorization-matrix coverage for the Wave 3a Final Media Reports endpoints
/// (API-SURFACE.md §16): the two intentionally-public reads stay anonymous, every mutating/AI
/// /PDF/email-cost route now requires authentication (closes DEFECT-LOG.md SEC-02 -- 7 of these
/// 12 routes were completely unauthenticated in the Node source, including the PDF-export and
/// email-send endpoints the audit specifically flagged), and the immutability guard
/// (PUT/DELETE) always returns 403 for every caller, matching the Node source's unconditional
/// rejection exactly.
/// </summary>
public sealed class FinalMediaReportsAuthorizationTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private static readonly string[] SingleTestRecipient = { "a@example.com" };

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListFinalMediaReports_NoToken_ReturnsOkBecauseIntentionallyPublic()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/final-media-reports", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK, "matches the Node source's own file header comment: list/get are intentionally public");
    }

    [Fact]
    public async Task GetFinalMediaReportById_NoTokenMissingId_ReturnsNotFoundNotUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/final-media-reports/999999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GenerateFinalMediaReport_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/final-media-reports/generate", UriKind.Relative),
            new { periodLabel = "يوليو 2026", dateFrom = DateTimeOffset.UtcNow.AddDays(-7), dateTo = DateTimeOffset.UtcNow });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: unauthenticated AI-cost endpoint");
    }

    [Fact]
    public async Task GenerateFinalMediaReport_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/final-media-reports/generate", UriKind.Relative),
            new { periodLabel = "يوليو 2026", dateFrom = DateTimeOffset.UtcNow.AddDays(-7), dateTo = DateTimeOffset.UtcNow });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task GenerateFinalMediaReport_SuperAdminNoSourceData_ReturnsUnprocessableEntity()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/final-media-reports/generate", UriKind.Relative),
            new { periodLabel = "يوليو 2026", dateFrom = DateTimeOffset.UtcNow.AddDays(-7), dateTo = DateTimeOffset.UtcNow });

        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity, "ports BUSINESS-RULES.md §5.3's NO_SOURCE_DATA guard: a fresh test database has zero posts/news");
    }

    [Fact]
    public async Task CreateFinalMediaReport_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(new Uri("/api/v1/final-media-reports", UriKind.Relative), new { title = "تقرير" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: the Node source required requireAdmin, this port requires the equivalent create policy");
    }

    [Fact]
    public async Task ExportPdf_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/final-media-reports/1/export-pdf", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: was an unauthenticated Puppeteer resource-cost endpoint flagged by the audit");
    }

    [Fact]
    public async Task SendEmail_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/final-media-reports/1/send-email", UriKind.Relative), new { recipients = SingleTestRecipient });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: was an unauthenticated email-cost abuse vector flagged by the audit");
    }

    [Fact]
    public async Task ExecSummary_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/final-media-reports/1/exec-summary", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: unauthenticated AI-cost endpoint");
    }

    [Fact]
    public async Task Search_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/final-media-reports/search", UriKind.Relative), new { mode = "full", query = "منافسة" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: unauthenticated AI-cost endpoint");
    }

    [Fact]
    public async Task QaQueries_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(new Uri("/api/v1/qa-queries", UriKind.Relative), new { period = "أسبوعي" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: was an unauthenticated write to an audit table, an attacker could pollute the audit log itself");
    }

    [Fact]
    public async Task QaQueries_SuperAdmin_ReturnsCreated()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/qa-queries", UriKind.Relative), new { period = "أسبوعي", mode = "generate" });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task SeedDemo_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/final-media-reports/seed-demo", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes SEC-02: the Node source used a manual inline role check instead of route-level auth");
    }

    [Fact]
    public async Task SeedDemo_SuperAdmin_ReturnsCreatedWithSeedCounts()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/final-media-reports/seed-demo", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
        SeedDemoResponsePayload? payload = await response.Content.ReadFromJsonAsync<SeedDemoResponsePayload>();
        payload!.SeededNews.Should().Be(6);
        payload.SeededPosts.Should().Be(6);
    }

    [Fact]
    public async Task DeleteFinalMediaReport_NoToken_AlwaysReturnsForbidden()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.DeleteAsync(new Uri("/api/v1/final-media-reports/1", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "immutability guard: matches final-media-reports.ts:795-797 exactly, always 403 regardless of caller identity");
    }

    [Fact]
    public async Task DeleteFinalMediaReport_SuperAdmin_StillReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.DeleteAsync(new Uri("/api/v1/final-media-reports/1", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "immutability is unconditional -- not even super-admin can delete a final report");
    }

    [Fact]
    public async Task PutFinalMediaReport_NoToken_AlwaysReturnsForbidden()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PutAsJsonAsync(new Uri("/api/v1/final-media-reports/1", UriKind.Relative), new { title = "محاولة تعديل" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "immutability guard: matches final-media-reports.ts:798-800 exactly, always 403 regardless of caller identity");
    }

    [Fact]
    public async Task PutFinalMediaReport_SuperAdmin_StillReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.PutAsJsonAsync(new Uri("/api/v1/final-media-reports/1", UriKind.Relative), new { title = "محاولة تعديل" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "immutability is unconditional -- not even super-admin can edit a final report");
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

    private sealed record SeedDemoResponsePayload(string Message, int SeededNews, int SeededPosts);
}
