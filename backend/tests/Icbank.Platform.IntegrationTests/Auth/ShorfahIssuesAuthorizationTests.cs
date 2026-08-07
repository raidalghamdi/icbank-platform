using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Authorization-matrix coverage for the Wave 4a Shorfah issue-lifecycle endpoints
/// (API-SURFACE.md §19): anonymous -> 401 on every route (SEC-02: zero anonymous mutating
/// endpoints), viewer (zero grants) -> 403, super-admin -> 200/201 as applicable.
/// </summary>
public sealed class ShorfahIssuesAuthorizationTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ListIssues_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListIssues_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task ListIssues_SuperAdmin_ReturnsOkWithEnvelope()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ListIssuesPayload? payload = await response.Content.ReadFromJsonAsync<ListIssuesPayload>();
        payload!.Page.Should().Be(1);
        payload.PageSize.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetIssueById_UnknownId_ReturnsNotFound()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useSuperAdmin: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/999999", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetIssueAdmin_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/1/admin", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetIssueAdmin_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/1/admin", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden, "the /admin variant requires the elevated shorfah:edit policy, not just shorfah:view");
    }

    [Fact]
    public async Task CreateIssue_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/shorfah/issues", UriKind.Relative), new { titleAr = "عدد تجريبي", month = 8, year = 2026 });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task CreateIssue_ViewerWithZeroGrants_ReturnsForbidden()
    {
        HttpClient client = await ArrangeAuthenticatedClientAsync(useViewer: true);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri("/api/v1/shorfah/issues", UriKind.Relative), new { titleAr = "عدد تجريبي", month = 8, year = 2026 });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task CollectIssue_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/issues/1/collect", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "closes AMBIGUOUS-API-3 in favor of requiring auth -- the Node source left this route requireAuth-only with no admin gate");
    }

    [Fact]
    public async Task Publish_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/issues/1/publish", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SendInitial_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/issues/1/send-initial", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "SEC-02: send-initial is a cost-abuse vector (fans out real email sends) and must never be anonymous");
    }

    [Fact]
    public async Task ExportDocx_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/1/docx", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportPdfHtml_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/1/pdf", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ExportPdfBinary_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/1/pdf.pdf", UriKind.Relative));

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

    private sealed record ListIssuesPayload(int Page, int PageSize, int Total);
}
