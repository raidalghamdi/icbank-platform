using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.IntegrationTests.Auth;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Api;

/// <summary>
/// Integration coverage for the binary-returning endpoints restored by the Rendering wave: real
/// PDF export (<c>FinalMediaReportsController.ExportPdfAsync</c>,
/// <c>ShorfahIssuesController.GetPdfBinaryAsync</c>), real DOCX export
/// (<c>ShorfahIssuesController.ExportDocxAsync</c>, <c>AiYearController.GetReportDataAsync</c>),
/// and the real ZIP stream (<c>AiYearController.GetActivationZipAsync</c>). Asserts content
/// type, non-trivial content length, and that the pre-existing authorization policy still
/// applies (401 without a token) for every one of them, plus a full success path where the
/// underlying data can be produced from an empty seeded database (the AI Year report, which
/// aggregates gracefully to zero counts with no activations).
/// </summary>
public sealed class RenderingEndpointsTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task ExportFinalMediaReportPdf_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/final-media-reports/1/export-pdf", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "media_monitoring:view authorization must still gate the now-real PDF renderer");
    }

    [Fact]
    public async Task ShorfahIssuePdfBinary_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/1/pdf.pdf", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "shorfah:view authorization must still gate the now-real PDF renderer");
    }

    [Fact]
    public async Task ShorfahIssueDocx_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/1/docx", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "shorfah:view authorization must still gate the now-real DOCX renderer");
    }

    [Fact]
    public async Task AiYearActivationZip_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/ai-year/activations/1/zip", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "ai_year:view authorization must still gate the now-real ZIP stream");
    }

    [Fact]
    public async Task AiYearReportDocx_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/ai-year/report", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "ai_year:view authorization must still gate the now-real DOCX report");
    }

    [Fact]
    public async Task AiYearReportDataJson_NoToken_ReturnsUnauthorized()
    {
        using HttpClient client = _factory.CreateClient();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/ai-year/report/data", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized, "the JSON data path carries the same ai_year:view policy as the binary report");
    }

    [Fact]
    public async Task AiYearReportDocx_SuperAdminEmptyDatabase_ReturnsRealDocxBytes()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/ai-year/report", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        var bytes = await response.Content.ReadAsByteArrayAsync();
        bytes.Length.Should().BeGreaterThan(1000, "a real .docx package (zip + XML parts) is never a handful of bytes");
        bytes[0].Should().Be(0x50, "the first byte of any zip-based OPC package (docx) is 'P' from the PK magic number");
        bytes[1].Should().Be(0x4B, "the second byte of the PK zip magic number");
    }

    [Fact]
    public async Task AiYearReportDataJson_SuperAdminEmptyDatabase_ReturnsJsonWithZeroCounts()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/ai-year/report/data", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/json");
        ReportDataPayload? payload = await response.Content.ReadFromJsonAsync<ReportDataPayload>();
        payload!.TotalActivations.Should().Be(0, "a fresh test database has zero AI Year activations");
    }

    [Fact]
    public async Task AiYearActivationZip_SuperAdminUnknownActivation_ReturnsNotFoundNotServerError()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/ai-year/activations/999999/zip", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "an authorized caller requesting a non-existent activation's archive must get 404, not a 500 from the stream writer");
    }

    [Fact]
    public async Task ShorfahIssuePdfBinary_SuperAdminUnknownIssue_ReturnsNotFoundNotServerError()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/999999/pdf.pdf", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ShorfahIssueDocx_SuperAdminUnknownIssue_ReturnsNotFoundNotServerError()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/issues/999999/docx", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task ExportFinalMediaReportPdf_SuperAdminUnknownReport_ReturnsNotFoundNotServerError()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/final-media-reports/999999/export-pdf", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private async Task<HttpClient> ArrangeSuperAdminClientAsync()
    {
        using IServiceScope scope = _factory.Services.CreateScope();
        Icbank.Platform.Infrastructure.Persistence.AppDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<Icbank.Platform.Infrastructure.Persistence.AppDbContext>();
        AuthTestDataBuilder.SeededUsers seeded = await AuthTestDataBuilder.SeedAsync(dbContext, SharedPassword);

        HttpClient client = _factory.CreateClient();
        HttpResponseMessage loginResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/auth/login", UriKind.Relative), new { email = seeded.SuperAdmin.Email, password = SharedPassword });
        loginResponse.EnsureSuccessStatusCode();

        LoginResponsePayload? payload = await loginResponse.Content.ReadFromJsonAsync<LoginResponsePayload>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", payload!.AccessToken);
        return client;
    }

    private sealed record LoginResponsePayload(string AccessToken);

    private sealed record ReportDataPayload(int TotalActivations, int TotalMedia, int TotalChannels);
}
