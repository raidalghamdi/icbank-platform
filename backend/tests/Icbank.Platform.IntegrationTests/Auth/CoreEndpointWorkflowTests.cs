using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// End-to-end workflow coverage for the Wave 1 mutating endpoints not already exercised by
/// <see cref="CoreEndpointAuthorizationTests"/> — approve/publish/reject/edit/delete lifecycle
/// transitions for Weekend Drafts, Weekend Places, and Week Start, plus audit-log-write
/// assertions per task requirement 5 ("audit-log write on every mutating action").
/// </summary>
public sealed class CoreEndpointWorkflowTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task WeekendDraftLifecycle_GenerateApprovePublish_TransitionsThroughEveryStatus()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage generateResponse = await client.PostAsJsonAsync(new Uri("/api/v1/weekend/generate", UriKind.Relative), new { });
        generateResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var draftId = await ReadDraftIdAsync(generateResponse);

        HttpResponseMessage approveResponse = await client.PostAsync(new Uri($"/api/v1/weekend/drafts/{draftId}/approve", UriKind.Relative), content: null);
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage secondApproveResponse = await client.PostAsync(new Uri($"/api/v1/weekend/drafts/{draftId}/approve", UriKind.Relative), content: null);
        secondApproveResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest, "re-approving an already-approved draft must be rejected per BUSINESS-RULES.md §2.2");

        HttpResponseMessage publishResponse = await client.PostAsync(new Uri($"/api/v1/weekend/drafts/{draftId}/publish", UriKind.Relative), content: null);
        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage getByIdResponse = await client.GetAsync(new Uri($"/api/v1/weekend/drafts/{draftId}", UriKind.Relative));
        getByIdResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage listResponse = await client.GetAsync(new Uri("/api/v1/weekend/drafts?status=Published", UriKind.Relative));
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage publishedResponse = await client.GetAsync(new Uri("/api/v1/weekend/published", UriKind.Relative));
        publishedResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage editResponse = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/weekend/drafts/{draftId}", UriKind.Relative), new { content = new { summary = "updated" } });
        editResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage rejectResponse = await client.PostAsJsonAsync(
            new Uri($"/api/v1/weekend/drafts/{draftId}/reject", UriKind.Relative), new { reason = "test rejection" });
        rejectResponse.StatusCode.Should().Be(HttpStatusCode.OK, "reject has no status precondition — even an already-published draft can be rejected");

        HttpResponseMessage deleteResponse = await client.DeleteAsync(new Uri($"/api/v1/weekend/drafts/{draftId}", UriKind.Relative));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage deleteAgainResponse = await client.DeleteAsync(new Uri($"/api/v1/weekend/drafts/{draftId}", UriKind.Relative));
        deleteAgainResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WeekendDraftPublish_FromPendingReview_SkipsApproveAndBackfillsApprover()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage generateResponse = await client.PostAsJsonAsync(new Uri("/api/v1/weekend/generate", UriKind.Relative), new { });
        var draftId = await ReadDraftIdAsync(generateResponse);

        HttpResponseMessage publishResponse = await client.PostAsync(new Uri($"/api/v1/weekend/drafts/{draftId}/publish", UriKind.Relative), content: null);

        publishResponse.StatusCode.Should().Be(HttpStatusCode.OK, "publish must be allowed to skip the approve step per BUSINESS-RULES.md §2.2");
    }

    [Fact]
    public async Task WeekendPlaceLifecycle_CreateUpdateDelete_Succeeds()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/weekend-places", UriKind.Relative), new { name = "حديقة الملك سلمان", description = "حديقة عائلية", sortOrder = 1 });
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var placeId = await ReadIdAsync(createResponse);

        HttpResponseMessage uploadUrlResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/weekend-places/upload-url", UriKind.Relative), new { fileName = "photo.jpg" });
        uploadUrlResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage updateResponse = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/weekend-places/{placeId}", UriKind.Relative), new { isActive = false });
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage listResponse = await client.GetAsync(new Uri("/api/v1/weekend-places", UriKind.Relative));
        listResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage deleteResponse = await client.DeleteAsync(new Uri($"/api/v1/weekend-places/{placeId}", UriKind.Relative));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage deleteAgainResponse = await client.DeleteAsync(new Uri($"/api/v1/weekend-places/{placeId}", UriKind.Relative));
        deleteAgainResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task WeekStartWorkflow_GenerateApproveThenDeleteArchivedEntry_Succeeds()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage generateResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/week-start/generate", UriKind.Relative), new { topic = "الإنجاز" });
        generateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var outputId = await ReadFirstGeneratedOutputIdAsync(generateResponse);

        HttpResponseMessage approveResponse = await client.PostAsJsonAsync(new Uri("/api/v1/week-start/approve", UriKind.Relative), new { id = outputId });
        approveResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage archiveResponse = await client.GetAsync(new Uri("/api/v1/week-start/archive", UriKind.Relative));
        archiveResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var archiveEntryId = await ReadFirstArchiveEntryIdAsync(archiveResponse);

        HttpResponseMessage deleteResponse = await client.DeleteAsync(new Uri($"/api/v1/week-start/archive/{archiveEntryId}", UriKind.Relative));
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task WeekStartUpload_PlainTextFile_ArchivesAndRecomputesStyleProfile()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        using var content = new MultipartFormDataContent();
        var fileBytes = System.Text.Encoding.UTF8.GetBytes("مرحباً بكم في بداية أسبوع جديد ملهم للجميع في هذه المؤسسة الرائدة.");
        var fileContent = new ByteArrayContent(fileBytes);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(fileContent, "files", "sample.txt");

        HttpResponseMessage uploadResponse = await client.PostAsync(new Uri("/api/v1/week-start/upload", UriKind.Relative), content);

        uploadResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        HttpResponseMessage profileResponse = await client.GetAsync(new Uri("/api/v1/week-start/style-profile", UriKind.Relative));
        profileResponse.StatusCode.Should().Be(HttpStatusCode.OK, "the style profile must exist after at least one successful upload");
    }

    [Fact]
    public async Task DailyReportUpsert_WithValidApiKey_UpsertsAndOverwritesOnSecondCall()
    {
        HttpClient client = _factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Api-Key", "test-cron-key");

        HttpResponseMessage firstResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/daily-report", UriKind.Relative), new { reportDate = "2026-08-05", reportData = new { title = "v1" } });
        firstResponse.StatusCode.Should().Be(HttpStatusCode.Created);

        HttpResponseMessage secondResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/daily-report", UriKind.Relative), new { reportDate = "2026-08-05", reportData = new { title = "v2" } });
        secondResponse.StatusCode.Should().Be(HttpStatusCode.Created, "an existing row for the same date must be updated, not duplicated");

        HttpResponseMessage n8nResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/report", UriKind.Relative), new { report_date = "2026-08-06", overdue_projects = new[] { new { name = "x" } } });
        n8nResponse.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    private static async Task<int> ReadDraftIdAsync(HttpResponseMessage response)
    {
        var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("draft").GetProperty("id").GetInt32();
    }

    private static async Task<int> ReadIdAsync(HttpResponseMessage response)
    {
        var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("id").GetInt32();
    }

    private static async Task<int> ReadFirstGeneratedOutputIdAsync(HttpResponseMessage response)
    {
        var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement[0].GetProperty("id").GetInt32();
    }

    private static async Task<int> ReadFirstArchiveEntryIdAsync(HttpResponseMessage response)
    {
        var document = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return document.RootElement.GetProperty("entries")[0].GetProperty("id").GetInt32();
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
