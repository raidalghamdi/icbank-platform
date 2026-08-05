using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Icbank.Platform.Domain.Shorfah;
using Icbank.Platform.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Icbank.Platform.IntegrationTests.Auth;

/// <summary>
/// Full lifecycle coverage for the Wave 4b Shorfah section-workflow endpoints (API-SURFACE.md
/// §19, BUSINESS-RULES.md §1.3): patch field-tiers, submit/review/approve transitions (including
/// the permissive, non-state-machine-enforced transitions AMBIGUOUS-BR-1 documents), AI
/// generation, and the workflow log.
/// </summary>
public sealed class ShorfahSectionWorkflowTests : IDisposable
{
    private const string SharedPassword = "Str0ng!Passw0rd#2026";

    private readonly AuthWebApplicationFactory _factory = new();

    /// <inheritdoc />
    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Submit_WithContent_TransitionsToSubmitted()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        await PatchContentAsync(client, section.Id, "محتوى تجريبي");

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/submit", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SectionPayload? payload = await response.Content.ReadFromJsonAsync<SectionPayload>();
        payload!.Section.WorkflowStatus.Should().Be(nameof(ShorfahWorkflowStatus.Submitted));
    }

    [Fact]
    public async Task Submit_WithoutContent_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/submit", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest, "BUSINESS-RULES.md §1.3 hard precondition: contentMd/contentHtml must be non-empty before submit");
    }

    [Fact]
    public async Task Submit_UnknownSection_ReturnsNotFound()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/sections/999999/submit", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Review_PassDecision_TransitionsToInReview()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/review", UriKind.Relative), new { decision = "pass", notes = "جيد" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SectionPayload? payload = await response.Content.ReadFromJsonAsync<SectionPayload>();
        payload!.Section.WorkflowStatus.Should().Be(nameof(ShorfahWorkflowStatus.InReview));
    }

    [Fact]
    public async Task Review_RejectDecision_TransitionsToRejected()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/review", UriKind.Relative), new { decision = "reject", notes = "يحتاج مراجعة" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SectionPayload? payload = await response.Content.ReadFromJsonAsync<SectionPayload>();
        payload!.Section.WorkflowStatus.Should().Be(nameof(ShorfahWorkflowStatus.Rejected));
    }

    [Fact]
    public async Task Review_FromPendingContribution_Succeeds_MatchingAmbiguousBr1()
    {
        // Why: BUSINESS-RULES.md §1.3 AMBIGUOUS-BR-1 -- review is not state-machine-enforced; a
        // reviewer can "review" a still-pending_contribution section, and this port preserves
        // that permissive behaviour verbatim rather than silently introducing a new guard.
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        section.WorkflowStatus.Should().Be(nameof(ShorfahWorkflowStatus.PendingContribution));

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/review", UriKind.Relative), new { decision = "pass" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Approve_FromAnyStatus_Succeeds_MatchingAmbiguousBr1()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PostAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}/approve", UriKind.Relative), new { notes = "تم" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        ApprovePayload? payload = await response.Content.ReadFromJsonAsync<ApprovePayload>();
        payload!.Section.WorkflowStatus.Should().Be(nameof(ShorfahWorkflowStatus.Approved));
    }

    [Fact]
    public async Task Generate_ProducesSubmittedSectionWithContent()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PostAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/generate", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SectionPayload? payload = await response.Content.ReadFromJsonAsync<SectionPayload>();
        payload!.Section.WorkflowStatus.Should().Be(nameof(ShorfahWorkflowStatus.Submitted));
        payload.Section.ContentMd.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Generate_UnknownSection_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PostAsync(new Uri("/api/v1/shorfah/sections/999999/generate", UriKind.Relative), content: null);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound, "the SEC-16 resource-existence guard runs before the handler's own not-found check");
    }

    [Fact]
    public async Task PatchSection_MetadataFieldsAsAdmin_Succeeds()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}", UriKind.Relative), new { titleAr = "عنوان جديد", slaDays = 10 });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        SectionPayload? payload = await response.Content.ReadFromJsonAsync<SectionPayload>();
        payload!.Section.TitleAr.Should().Be("عنوان جديد");
        payload.Section.SlaDays.Should().Be(10);
    }

    [Fact]
    public async Task PatchSection_InvalidSlaDaysOutOfBounds_ReturnsBadRequest()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri($"/api/v1/shorfah/sections/{section.Id}", UriKind.Relative), new { slaDays = 999 });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task PatchSection_UnknownSection_ReturnsNotFound()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.PatchAsJsonAsync(
            new Uri("/api/v1/shorfah/sections/999999", UriKind.Relative), new { titleAr = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetLog_AfterSubmitAndApprove_ContainsBothEntries()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);
        await PatchContentAsync(client, section.Id, "محتوى");
        await client.PostAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/submit", UriKind.Relative), content: null);
        HttpResponseMessage approveResponse = await client.PostAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/approve", UriKind.Relative), content: null);
        approveResponse.EnsureSuccessStatusCode();

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/log?page=1&pageSize=10", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LogPayload? payload = await response.Content.ReadFromJsonAsync<LogPayload>();
        payload!.Logs.Should().Contain(l => l.Action == "submitted");
        payload.Logs.Should().Contain(l => l.Action == "approved");
        payload.Total.Should().BeGreaterOrEqualTo(2);
    }

    [Fact]
    public async Task GetLog_EmptySection_ReturnsEmptyEnvelope()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();
        SectionDto section = await FirstSectionAsync(client);

        HttpResponseMessage response = await client.GetAsync(new Uri($"/api/v1/shorfah/sections/{section.Id}/log?page=1&pageSize=10", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        LogPayload? payload = await response.Content.ReadFromJsonAsync<LogPayload>();
        payload!.Logs.Should().BeEmpty();
        payload.Total.Should().Be(0);
    }

    [Fact]
    public async Task GetLog_UnknownSection_ReturnsNotFound()
    {
        HttpClient client = await ArrangeSuperAdminClientAsync();

        HttpResponseMessage response = await client.GetAsync(new Uri("/api/v1/shorfah/sections/999999/log", UriKind.Relative));

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task PatchContentAsync(HttpClient client, int sectionId, string contentMd) =>
        (await client.PatchAsJsonAsync(new Uri($"/api/v1/shorfah/sections/{sectionId}", UriKind.Relative), new { contentMd })).EnsureSuccessStatusCode();

    private static async Task<SectionDto> FirstSectionAsync(HttpClient client)
    {
        HttpResponseMessage createResponse = await client.PostAsJsonAsync(
            new Uri("/api/v1/shorfah/issues", UriKind.Relative), new { titleAr = $"عدد اختبار {Guid.NewGuid()}", month = 8, year = 2026 });
        createResponse.EnsureSuccessStatusCode();
        CreateIssuePayload? issue = await createResponse.Content.ReadFromJsonAsync<CreateIssuePayload>();

        HttpResponseMessage detailResponse = await client.GetAsync(new Uri($"/api/v1/shorfah/issues/{issue!.Issue.Id}", UriKind.Relative));
        GetIssuePayload? detail = await detailResponse.Content.ReadFromJsonAsync<GetIssuePayload>();
        return detail!.Sections[0];
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

    private sealed record IssueDto(int Id, string TitleAr, string Status);

    private sealed record SectionDto(int Id, string TitleAr, string WorkflowStatus, string? ContentMd, int? SlaDays);

    private sealed record CreateIssuePayload(IssueDto Issue);

    private sealed record GetIssuePayload(IssueDto Issue, List<SectionDto> Sections);

    private sealed record SectionPayload(SectionDto Section);

    private sealed record ApprovePayload(SectionDto Section);

    private sealed record LogEntryDto(int Id, string Action);

    private sealed record LogPayload(List<LogEntryDto> Logs, int Page, int PageSize, int Total);
}
